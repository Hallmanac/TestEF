using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using TestEf.Console.Core;
using TestEf.Console.Identity;

namespace TestEf.Console.Repo
{
    public class UserRepo : BaseEntitySqlRepo<User, MainDbContext>
    {
        public override async Task<List<User>> GetByIdsAsync(int[] ids)
        {
            using(Context = new MainDbContext())
            {
                return await Context.Users
                                    .Include(usr => usr.Emails)
                                    .Include(usr => usr.PhoneNumbers)
                                    .Where(usr => ids.Any(id => id == usr.Id))
                                    .ToListAsync().ConfigureAwait(false);
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
                                    .Where(usr => userNames.Any(un => un == usr.Username))
                                    .ToListAsync().ConfigureAwait(false);
            }
        }

        public async Task<User> GetByUsernameAsync(string username)
        {
            var users = await GetByUserNamesAsync(new List<string> {username}).ConfigureAwait(false);
            return users.FirstOrDefault(usr => string.Equals(usr.Username, username, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Saves a set of objects, including its child collection, as an insert, update, or delete operation depending on what has changed from in the database.
        /// </summary>
        /// <param name="entities"></param>
        public override async Task SaveFullEntitiesAsync(User[] entities)
        {
            if(entities == null || entities.Length < 1)
            {
                return;
            }
            var users = entities.ToList();
            await SaveEmailsAsync(users.Where(u => u.Id != 0).SelectMany(u => u.Emails).ToList()).ConfigureAwait(false);
            await SavePhoneNumbersAsync(users.Where(u => u.Id != 0).SelectMany(u => u.PhoneNumbers).ToList(), entities).ConfigureAwait(false);
            await UpdateCollectionAsync(users).ConfigureAwait(false);
        }

        public async Task SavePhoneNumbersAsync(List<PhoneNumber> givenItems, User[] givenUsers)
        {
            var userIds = givenUsers.Select(usr => usr.Id).ToList();
            await DeleteRemovedManyToManyCollections(givenItems, givenUsers.ToList()).ConfigureAwait(false);
            await UpdateCollectionAsync(givenItems).ConfigureAwait(false);
        }

        public async Task SaveEmailsAsync(List<Email> givenItems)
        {
            await DeleteRemovedUserCollectionsAsync(givenItems).ConfigureAwait(false);
            await UpdateCollectionAsync(givenItems).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes each of the objects in the given array
        /// </summary>
        /// <param name="entities"></param>
        public override Task DeleteAsync(User[] entities) { return null; }

        public async Task DeleteRemovedUserCollectionsAsync<TUserCollection>(List<TUserCollection> givenItems)
            where TUserCollection : class, IBaseEntity, IEquatable<TUserCollection>, IUserCollection, new()
        {
            /*
             * We need to:
             *     1) From the database, get all "Collection Items" that have the UserId of any of the givenItems 
             *        (hence the IUserCollection interface to guarantee the existence of a UserId property)
             *     2) Of the database items that come back remove remove any items that are equal to any of the givenItems
             *     3) The database items that are remaining are what was deleted while processing logic was being run (a.k.a. a web request was
             *        being handled).
             *     4) On the remaining items, call the SaveSqlEntitiesAsBatchAsync method with the EntityState.Deleted being passed in.
             */
            List<TUserCollection> itemsToDelete;
            var userEntityIds = givenItems.Select(item => item.UserId).ToList();
            using(Context = new MainDbContext())
            {
                itemsToDelete = await Context.Set<TUserCollection>().Where(uc => userEntityIds.Any(id => id == uc.UserId))
                                             .ToListAsync()
                                             .ConfigureAwait(false);
                itemsToDelete.RemoveAll(item => givenItems.Any(gi => gi.Id == item.Id));
            }
            await SaveSqlEntitiesAsBatchAsync(itemsToDelete.ToArray(), EntityState.Deleted).ConfigureAwait(false);
        }

        public async Task DeleteRemovedManyToManyCollections<TUserManyToManyCollection>(List<TUserManyToManyCollection> givenItems, List<User> users)
            where TUserManyToManyCollection : class, IBaseEntity, IEquatable<TUserManyToManyCollection>, IUserManyToManyCollection, new()
        {
            List<TUserManyToManyCollection> itemsToDelete;
            var userIds = users.Select(usr => usr.Id).ToList();
            var givenIds = givenItems.Select(gi => gi.Id).ToList();
            using(Context = new MainDbContext())
            {
                itemsToDelete = await (from mtmCollection in Context.Set<TUserManyToManyCollection>()
                                       where (
                                           (from gId in givenIds
                                            where gId == mtmCollection.Id
                                            select gId)
                                           ).Any()
                                       select mtmCollection).ToListAsync().ConfigureAwait(false)
                    ;

                //itemsToDelete = await Context.Set<TUserManyToManyCollection>()
                //                             .Where(mtmC => mtmC.Users.Contains())
                //                             .ToListAsync().ConfigureAwait(false);
                itemsToDelete.RemoveAll(item => givenItems.Any(gi => gi.Id == item.Id));
            }
            await SaveSqlEntitiesAsBatchAsync(itemsToDelete.ToArray(), EntityState.Deleted).ConfigureAwait(false);
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