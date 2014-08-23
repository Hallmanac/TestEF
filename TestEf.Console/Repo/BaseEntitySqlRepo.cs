﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using TestEf.Console.Core;

namespace TestEf.Console.Repo
{
    public abstract class BaseEntitySqlRepo<TModelObject, TContext> : IBaseEntityRepo<TModelObject> where TModelObject : class, IBaseEntity, new()
                                                                                                    where TContext : DbContext, new()
    {
        protected TContext Context;

        #region --- Dispose implementation ---
        private bool _disposed;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Pattern for handling multiple calls to Dispose and allowing sub-classes to implment their own dispose pattern that could
        /// also include handling any calls to finalize from the GC.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if(_disposed)
            {
                return;
            }
            if(!disposing)
            {
                return;
            }
            if(Context != null)
            {
                Context.Dispose();
                Context = null;
            }
            _disposed = true;
        }
        #endregion

        /// <summary>
        /// Returns an IQueryable<typeparam name="TModelObject"></typeparam>. This is more for use with Entity Framework or
        /// providers like it that support IQueryable. Azure Table Storage doesn't currently support IQueryable (as of 2013-08-20) so
        /// the implementations of this might return the same thing as the GetEnumerable() method until such time that the IQueryable support 
        /// for Azure Table Storage is fully implemented.
        /// </summary>
        /// <returns></returns>
        public IQueryable<TModelObject> DbSet()
        {
            Context = new TContext();
            Context.Configuration.AutoDetectChangesEnabled = true;
            Context.Configuration.LazyLoadingEnabled = true;
            Context.Configuration.ProxyCreationEnabled = true;
            return Context.Set<TModelObject>();
        }

        public abstract Task<List<TModelObject>> GetByIdsAsync(int[] ids); 

        /// <summary>
        /// Gets an object by its Guid based ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public abstract Task<TModelObject> GetByIdAsync(int id);

        /// <summary>
        /// Saves a set of objects (along with all their child collection tables) as an insert operation.
        /// </summary>
        /// <param name="entities"></param>
        public async Task InsertAsync(TModelObject[] entities)
        {
            // When inserting, Entity Framework automatically inserts any new child collections or child objects as well
            // so there's no need to call "SaveAllChildCollections"
            if(entities == null)
            {
                throw new ArgumentNullException("entities");
            }
            if(entities.Length < 1)
            {
                return;
            }
            await SaveSqlEntitiesAsBatchAsync(entities, EntityState.Added).ConfigureAwait(false);
        }

        /// <summary>
        /// Saves a set of objects, including its child collection, as an insert, update, or delete operation depending on what has changed from in the database.
        /// </summary>
        /// <param name="entities"></param>
        public abstract Task SaveFullEntitiesAsync(TModelObject[] entities);

        /// <summary>
        /// Saves an entity to it's associated table only, excluding any collections or related tables.
        /// </summary>
        /// <param name="entities"></param>
        public async Task UpdateBasicEntitiesAsync(TModelObject[] entities)
        {
            if(entities == null)
            {
                throw new ArgumentNullException("entities");
            }
            if(entities.Length < 1)
            {
                return;
            }
            await SaveSqlEntitiesAsBatchAsync(entities, EntityState.Modified).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes each of the objects in the given array
        /// </summary>
        /// <param name="entities"></param>
        public abstract Task DeleteAsync(TModelObject[] entities);

        /// <summary>
        /// Saves an array of entities as a batch Insert, Update, or Delete based on the given EntityState.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities">Array of entities to save as batch to the database</param>
        /// <param name="entState">EntityState.Added, EntityState.Modified, EntityState.Deleted, etc.</param>
        public virtual async Task SaveSqlEntitiesAsBatchAsync<TEntity>(TEntity[] entities, EntityState entState) where TEntity : class, IBaseEntity, new()
        {
            if(entities == null)
            {
                throw new ArgumentNullException("entities");
            }
            if(entities.Length < 1)
            {
                return;
            }

            // Create a maximum batch count that is 100 or the number of entities to save. Which ever is less.
            // The number 100 is based on several StackOverflow posts that have indicated that their benchmarks for batch saves were best optimized at 100
            var commitCount = (entities.Length) < 100 ? entities.Length : 100;

            // List to hold the current batch insert
            var commitList = new List<TEntity>();
            var remainingCommits = entities.Length;
            foreach(var entity in entities)
            {
                // Set the last modified on
                entity.LastModifiedOn = DateTimeOffset.UtcNow;

                // Add the current iteration entity to the current batch to be saved.
                commitList.Add(entity);
                remainingCommits--;
                if(commitList.Count % commitCount != 0 && remainingCommits != 0)
                {
                    continue; // Keep building the commitList until we've met the commit count and then we'll go to the database
                }

                // Now that we've reached the max count for the current batch save, we save.
                using(Context = new TContext())
                {
                    // We make double sure that AutoDetectChanges is off because that has a pretty significant impact on raw performance
                    Context.Configuration.AutoDetectChangesEnabled = false;
                    if(entState == EntityState.Added)
                    {
                        Context.Set<TEntity>().AddRange(commitList);
                    }
                    else
                    {
                        // Iterate through the commitList and change the entity State to "Added" which will attach the entity to the DbContext (Context)
                        commitList.ForEach(listEntity => Context.Entry(listEntity).State = entState);
                    }

                    // Commit to the database.
                    await Context.SaveChangesAsync().ConfigureAwait(false);
                }

                // We clear out the commitList in the event we have more entities to be saved.
                commitList.Clear();
            }
        }

        /// <summary>
        /// Saves a collection that is a child of the current <see cref="TModelObject"/> by getting current database values for 
        /// items with the same ID and comparing differences to determine whether an insert, update, or delete is required.
        /// </summary>
        /// <typeparam name="TEntity">Type of the Child collection of the <see cref="TModelObject"/></typeparam>
        /// <param name="givenItems">The items to be saved.</param>
        /// <returns>Async Task</returns>
        public virtual async Task UpdateCollectionAsync<TEntity>(List<TEntity> givenItems)
            where TEntity : class, IBaseEntity, IEquatable<TEntity>, new()
        {
            // Get the database versions of the givenItems for comparison. 
            List<TEntity> dbItems;
            var givenIds = givenItems.Select(gi => gi.Id).ToList();
            using (Context = new TContext())
            {
                dbItems = await Context.Set<TEntity>().Where(dbItem => givenIds.Any(givenId => givenId == dbItem.Id)).ToListAsync().ConfigureAwait(false);
            }

            // Loop through the givenItems to see which ones need an update and which ones need an insert
            var inserts = givenItems.Where(gi => gi.Id == 0).ToList();
            var updates = (from givenItem in givenItems
                           let dbItem = dbItems.FirstOrDefault(di => di.Id == givenItem.Id)
                           where !givenItem.Equals(dbItem)
                           select givenItem).ToList();
            
            // We find which items need to be deleted by getting all the dbItems that aren't in the givenItems
            var deletes = dbItems.Where(dbItem => givenItems.All(givenItem => givenItem.Id != dbItem.Id)).ToList();

            if (deletes.Count > 0)
            {
                await SaveSqlEntitiesAsBatchAsync(deletes.ToArray(), EntityState.Deleted).ConfigureAwait(false);
            }
            if (inserts.Count > 0)
            {
                await SaveSqlEntitiesAsBatchAsync(inserts.ToArray(), EntityState.Added).ConfigureAwait(false);
            }
            if (updates.Count > 0)
            {
                await SaveSqlEntitiesAsBatchAsync(updates.ToArray(), EntityState.Modified).ConfigureAwait(false);
            }
        }

        protected int GetCommitCount(TModelObject[] entities)
        {
            // Create a maximum batch count that is 100 or the number of entities to save. Which ever is less.
            // The number 100 is based on several StackOverflow posts that have indicated that their benchmarks for batch saves were best optimized at 100
            return (entities.Length) < 100 ? entities.Length : 100;
        }
    }
}