using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TestEf.Console.Core;
using TestEf.Console.Core.ExtensionMethods;
using TestEf.Console.Identity;

namespace TestEf.Console.Repo
{
    public class UserRepo : BaseEntitySqlRepo<User, MainDbContext>
    {
        public override async Task<List<User>> GetByIdsAsync(int[] ids)
        {
            using(Context = new MainDbContext())
            {
                /*
                 * The "Any()" call does not work with Entity Framework 6.1.1 due to the query optimizer throwing an exception since it recognizes that
                 * the SQL that it will generate has poor performance
                */
                var users = await Context.Users.Include(usr => usr.Emails)
                                         .Include(usr => usr.PhoneNumbers)
                                         .Where(usr => ids.Contains(usr.Id)) // --- Contains() is all that works for this kind of query!!!!
                                         .ToListAsync()
                                         .ConfigureAwait(false);
                return users;
            }
        }

        /// <summary>
        /// Gets an object by its Guid based ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override async Task<User> GetByIdAsync(int id)
        {
            using(Context = new MainDbContext())
            {
                return await Context.Users
                                    .Include(usr => usr.Emails)
                                    .Include(usr => usr.PhoneNumbers)
                                    .FirstOrDefaultAsync(usr => usr.Id == id).ConfigureAwait(false);
            }
        }

        public async Task<List<User>> GetByUserNamesAsync(List<string> userNames)
        {
            using(Context = new MainDbContext())
            {
                return await Context.Users
                                    .Include(usr => usr.PhoneNumbers)
                                    .Include(usr => usr.Emails)
                                    .Where(usr => userNames.Contains(usr.Username)) // --- Only use Contains() not Any()
                                    .ToListAsync().ConfigureAwait(false);
            }
        }

        public async Task<User> GetByUsernameAsync(string username)
        {
            var users = await GetByUserNamesAsync(new List<string> {username}).ConfigureAwait(false);
            return users.FirstOrDefault(usr => string.Equals(usr.Username, username, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Saves a set of objects (along with all their child collection tables) as an insert operation.
        /// </summary>
        /// <param name="entities"></param>
        public override async Task InsertAsync(List<User> entities)
        {
            // When inserting, Entity Framework automatically inserts any new child collections or child objects as well
            // so there's no need to call "SaveAllChildCollections"
            if (entities == null)
            {
                throw new ArgumentNullException("entities");
            }
            if (entities.Count < 1)
            {
                return;
            }
            await SaveSqlEntitiesAsBatchAsync(entities, EntityState.Added).ConfigureAwait(false);
            var entIds = entities.Select(ent => ent.Id).ToList();
            var newEnts = await GetByIdsAsync(entIds.ToArray()).ConfigureAwait(false);
            entities.ForEach(ent =>
            {
                var newEnt = newEnts.FirstOrDefault(nw => nw.Id == ent.Id);
                if(newEnt == null)
                {
                    return;
                }
                ent.PhoneNumbers = newEnt.PhoneNumbers;
                ent.Emails = newEnt.Emails;
            });
        }

        /// <summary>
        /// Saves a set of objects, including its child collection, as an insert, update, or delete operation depending on what has changed from in the database.
        /// </summary>
        /// <param name="entities"></param>
        public override async Task SaveFullEntitiesAsync(List<User> entities)
        {
            if(entities == null || entities.Count < 1)
            {
                return;
            }
            //var users = entities.ToList();
            var emailsToSave = entities.Where(u => u.Id != 0).SelectMany(u => u.Emails).Distinct().ToList();
            await SaveEmailsAsync(emailsToSave).ConfigureAwait(false);
            var phNmbrs = entities.Where(u => u.Id != 0).SelectMany(u => u.PhoneNumbers).Distinct().ToList();
            await SavePhoneNumbersAsync(phNmbrs, entities).ConfigureAwait(false);
            await UpdateCollectionAsync(entities).ConfigureAwait(false);
        }

        public async Task SavePhoneNumbersAsync(List<PhoneNumber> givenItems, List<User> givenUsers)
        {
            await DeleteRemovedManyToManyCollections(givenItems, givenUsers.ToList()).ConfigureAwait(false);
            await UpdateCollectionAsync(givenItems).ConfigureAwait(false);
        }

        public async Task SaveEmailsAsync(List<Email> givenItems)
        {
            var sw = new Stopwatch();
            sw.Start();
            await DeleteRemovedUserCollectionsAsync(givenItems).ConfigureAwait(false);
            sw.Stop();
            System.Console.WriteLine("\nElapsed time for Delete Removed Emails was {0}", sw.ElapsedMilliseconds);
            await UpdateCollectionAsync(givenItems).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes each of the objects in the given array
        /// </summary>
        /// <param name="entities"></param>
        public override async Task DeleteAsync(List<User> entities)
        {
            if(entities == null || entities.Count < 1)
            {
                return;
            }
            /*
             * Need to delete the property collections from the database and then clear those lists out before deleting the UserEntity, otherwise you'll get a 
             * foreign key exception because EF will see that there are other collections that are attached to the UserEntity, do another insert of those 
             * collection items prior to deleting the user, and then throw a foreign key exception.
            */

            // Delete the Emails from the Database
            await SaveSqlEntitiesAsBatchAsync(entities.Where(usr => usr.Id != 0)
                                                       .SelectMany(usr => usr.Emails).Distinct().ToList(), EntityState.Deleted).ConfigureAwait(false);
            entities.ForEach(usr => usr.Emails.Clear());

            // Delete the PhoneNumbers from the Database
            var phsToDelete = entities.Where(user => user.Id != 0).SelectMany(usr => usr.PhoneNumbers).Distinct().ToList();
            await SaveSqlEntitiesAsBatchAsync(phsToDelete.ToList(), EntityState.Deleted).ConfigureAwait(false);
            entities.ForEach(usr => usr.PhoneNumbers.Clear());
            await SaveSqlEntitiesAsBatchAsync(entities, EntityState.Deleted).ConfigureAwait(false);
        }

        public async Task DeleteRemovedUserCollectionsAsync<TUserCollection>(List<TUserCollection> givenItems)
            where TUserCollection : class, IBaseEntity, IEquatable<TUserCollection>, IUserCollection, new()
        {
            /*
             * We need to:
             *     1) From the database, get all "Collection Items" that have the UserId of any of the givenItems 
             *        (hence the IUserCollection interface to guarantee the existence of a UserId property)
             *     2) Of the database items that come back, remove any items that are equal to any of the givenItems (i.e. remove items from the "to be deleted" 
             *          list that still exist and aren't being deleted)
             *     3) The database items that are remaining are what was deleted while processing logic was being run (a.k.a. a web request was
             *        being handled).
             *     4) On the remaining items, call the SaveSqlEntitiesAsBatchAsync method with the EntityState.Deleted being passed in.
            */

            // Batch the given items into groups of no more than 500
            var batchedItems = givenItems.ToBatch(500);
            foreach(var givenBatch in batchedItems)
            {

                var itemsToDelete = new List<TUserCollection>();
                var userEntityIds = givenBatch.Select(item => item.UserId).Distinct().ToList();
                using (Context = new MainDbContext())
                {
                    var query = from collectionItem in Context.Set<TUserCollection>()
                                where userEntityIds.Contains(collectionItem.UserId)
                                select collectionItem;
                    var dbItems = await query.ToListAsync().ConfigureAwait(false);
                    itemsToDelete.AddRange(dbItems);
                }
                itemsToDelete.RemoveAll(item => givenBatch.Any(gi => gi.Id == item.Id));
                itemsToDelete = itemsToDelete.Distinct().ToList();
                await SaveSqlEntitiesAsBatchAsync(itemsToDelete, EntityState.Deleted).ConfigureAwait(false);
            }
        }

        public async Task DeleteRemovedManyToManyCollections<TUserManyToManyCollection>(List<TUserManyToManyCollection> givenItems, List<User> users)
            where TUserManyToManyCollection : class, IBaseEntity, IEquatable<TUserManyToManyCollection>, IUserManyToManyCollection, new()
        {
            var itemsToDelete = new List<TUserManyToManyCollection>();
            var userIds = users.Select(usr => usr.Id).ToList();
            
            var sw = new Stopwatch();
            sw.Start();
            using(Context = new MainDbContext())
            {
                var query = (from mtmColl in Context.Set<TUserManyToManyCollection>()
                            where ((from user in Context.Users
                                    from phUser in mtmColl.Users
                                    where userIds.Contains(user.Id) && phUser.Id == user.Id
                                    select user).Any())
                            select mtmColl).Distinct();
                var dbList = await query.ToListAsync().ConfigureAwait(false);
                dbList.RemoveAll(dbItem => givenItems.Any(gi => gi.Id == dbItem.Id));
                itemsToDelete.AddRange(dbList);
            }
            await SaveSqlEntitiesAsBatchAsync(itemsToDelete, EntityState.Deleted).ConfigureAwait(false);
            sw.Stop();
            System.Console.WriteLine("\nElapsed update time in milliseconds was {0}", sw.ElapsedMilliseconds);

            #region --- Old way (commented out) ---
            //var sw = new Stopwatch();
            //sw.Start();
            //foreach(var uId in userIds)
            //{
            //    using (Context = new MainDbContext())
            //    {
            //        var dbItems = await (from mtmCollection in Context.Set<TUserManyToManyCollection>()
            //                             where mtmCollection.Users.Any(usr => usr.Id == uId)
            //                             select mtmCollection).ToListAsync().ConfigureAwait(false);
            //
            //        //itemsToDelete = await Context.Set<TUserManyToManyCollection>()
            //        //                             .Where(mtmC => mtmC.Users.Contains())
            //        //                             .ToListAsync().ConfigureAwait(false);
            //        dbItems.RemoveAll(dbItem => givenItems.Any(gi => gi.Id == dbItem.Id));
            //        itemsToDelete.AddRange(dbItems);
            //    }
            //    await SaveSqlEntitiesAsBatchAsync(itemsToDelete.ToArray(), EntityState.Deleted).ConfigureAwait(false);
            //    itemsToDelete.Clear();
            //}
            //sw.Stop();
            //System.Console.WriteLine("\nElapsed update time in milliseconds was {0}", sw.ElapsedMilliseconds);
            #endregion
        }

        public async Task<List<int>> GetAllUserIds()
        {
            using(Context = new MainDbContext())
            {
                return await Context.Users.Select(usr => usr.Id).ToListAsync().ConfigureAwait(false);
            }
        }
    }

    public interface IUserCollection
    {
        int UserId { get; set; }
    }

    public interface IUserManyToManyCollection
    {
        List<User> Users { get; set; }
    }
}