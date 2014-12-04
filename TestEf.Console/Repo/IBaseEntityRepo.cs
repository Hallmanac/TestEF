using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Threading.Tasks;
using TestEf.ConsoleMain.Core;

namespace TestEf.ConsoleMain.Repo
{
    public interface IBaseEntityRepo<TModelObject, out TContext> : IDisposable where TModelObject : class, IBaseEntity, new() where TContext : DbContext, new()
    {
        TContext DbContext();
        
        /// <summary>
        /// Returns an IQueryable<typeparam name="TModelObject"></typeparam>. This is more for use with Entity Framework or
        /// providers like it that support IQueryable. Azure Table Storage doesn't currently support IQueryable (as of 2013-08-20) so
        /// the implementations of this might return the same thing as the GetEnumerable() method until such time that the IQueryable support 
        /// for Azure Table Storage is fully implemented.
        /// </summary>
        /// <returns></returns>
        DbSet<TModelObject> DbSet();

        Task<List<TModelObject>> GetByIdsAsync(int[] ids);

        /// <summary>
        /// Gets an object by its Guid based ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TModelObject> GetByIdAsync(int id);

        /// <summary>
        /// Saves a set of objects as an insert operation.
        /// </summary>
        Task InsertAsync(List<TModelObject> entities);

        /// <summary>
        /// Saves a set of objects, including its child collection, as an insert, update, or delete operation depending on what has changed from in the database.
        /// </summary>
        Task SaveFullEntitiesAsync(List<TModelObject> entities);

        /// <summary>
        /// Saves an entity to it's associated table only, excluding any collections or related tables.
        /// </summary>
        Task UpdateBasicEntitiesAsync(List<TModelObject> entities);

        /// <summary>
        /// Deletes each of the objects in the given array
        /// </summary>
        Task DeleteAsync(List<TModelObject> entities);

        /// <summary>
        /// Saves any collection by getting current database values for 
        /// items with the same ID and comparing differences to determine whether an insert, update, or delete is required.
        /// The collection type must implement IBaseEntity and IEquality<typeparam name="TEntity"></typeparam>.
        /// </summary>
        /// <typeparam name="TEntity">Type of the Child collection of the <see cref="TModelObject"/></typeparam>
        /// <param name="givenItems">The items to be saved.</param>
        /// <returns>Async Task</returns>
        Task UpdateCollectionAsync<TEntity>(List<TEntity> givenItems)
            where TEntity : class, IBaseEntity, IEquatable<TEntity>, new();

        /// <summary>
        /// Saves an array of entities as a batch Insert, Update, or Delete based on the given EntityState.
        /// </summary>
        Task SaveSqlEntitiesAsBatchAsync<TEntity>(List<TEntity> entities, EntityState state) where TEntity : class, new();
    }
}