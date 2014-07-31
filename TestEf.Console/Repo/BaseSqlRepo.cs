namespace TestEf.Console.Repo
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public abstract class BaseSqlRepo<TModelObject, TContext> : IBaseEntityRepo<TModelObject> where TModelObject : class, IBaseEntity, new() where TContext : DbContext, new()
    {
        protected TContext Context;

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
            if (_disposed)
                return;
            if (!disposing)
            {
                return;
            }
            if (Context != null)
            {
                Context.Dispose();
                Context = null;
            }
            _disposed = true;
        }

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

        /// <summary>
        /// Gets an object by its Guid based ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public abstract Task<TModelObject> GetByIdAsync(int id = 0);
        
        /// <summary>
        /// Gets an object by its string based ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<TModelObject> GetByIdAsync(string id = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            var deserializeObject = JsonConvert.DeserializeObject<int>(id);
            return deserializeObject == default(int) ? null : await GetByIdAsync(deserializeObject).ConfigureAwait(false);
        }

        public async Task InsertAsync(TModelObject[] entities)
        {
            // When inserting, Entity Framework automatically inserts any new child collections or child objects as well
            // so there's no need to call "SaveAllChildCollections"
            if (entities == null)
            {
                throw new ArgumentNullException("entities");
            }
            if (entities.Length < 1)
            {
                return;
            }
            await SaveSqlEntitiesAsBatchAsync(entities, EntityState.Added).ConfigureAwait(false);
        }

        public Task UpdateBasicAsync(TModelObject[] entities) { return null; }

        public virtual async Task UpdateFullAsync(TModelObject[] entities)
        {
            if (entities == null)
            {
                throw new ArgumentNullException("entities");
            }
            if (entities.Length < 1)
            {
                return;
            }
            await SaveAllChildCollectionsAsync(entities).ConfigureAwait(false);
            await SaveSqlEntitiesAsBatchAsync(entities, EntityState.Modified).ConfigureAwait(false);
        }

        public abstract Task DeleteAsync(TModelObject[] entities);
        
        public virtual async Task SaveSqlEntitiesAsBatchAsync<TEntity>(TEntity[] entities, EntityState entState)
            where TEntity : class, IBaseEntity, new()
        {
            if (entities == null)
            {
                throw new ArgumentNullException("entities");
            }
            if (entities.Length < 1)
            {
                return;
            }

            // Create a maximum batch count that is 100 or the number of entities to save. Which ever is less.
            // The number 100 is based on several StackOverflow posts that have indicated that their benchmarks for batch saves were best optimized at 100
            var commitCount = (entities.Length) < 100 ? entities.Length : 100;

            // List to hold the current batch insert
            var commitList = new List<TEntity>();
            foreach (var entity in entities)
            {
                // Set the last modified on
                entity.LastModifiedOn = DateTimeOffset.UtcNow;

                // Add the current iteration entity to the current batch to be saved.
                commitList.Add(entity);
                if (commitList.Count % commitCount != 0)
                {
                    continue; // Keep building the commitList until we've met the commit count and then we'll go to the database
                }

                // Now that we've reached the max count for the current batch save, we save.
                using (Context = new TContext())
                {
                    // We make double sure that AutoDetectChanges is off because that has a pretty significant impact on raw performance
                    Context.Configuration.AutoDetectChangesEnabled = false;

                    if (entState == EntityState.Added)
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

        protected int GetCommitCount(TModelObject[] entities) { return (entities.Length) < 100 ? entities.Length : 100; }

        public abstract Task SaveAllChildCollectionsAsync(TModelObject[] entities);
    }
}