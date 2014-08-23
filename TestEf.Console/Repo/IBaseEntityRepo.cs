using TestEf.Console.Core;

namespace TestEf.Console.Repo
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public interface IBaseEntityRepo<TModelObject> : IDisposable where TModelObject : class, IBaseEntity, new()
    {
        /// <summary>
        /// Returns an IQueryable<typeparam name="TModelObject"></typeparam>. This is more for use with Entity Framework or
        /// providers like it that support IQueryable. Azure Table Storage doesn't currently support IQueryable (as of 2013-08-20) so
        /// the implementations of this might return the same thing as the GetEnumerable() method until such time that the IQueryable support 
        /// for Azure Table Storage is fully implemented.
        /// </summary>
        /// <returns></returns>
        IQueryable<TModelObject> DbSet();
        
        /// <summary>
        /// Gets an object by its Guid based ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TModelObject> GetByIdAsync(int id = default(int));

        /// <summary>
        /// Gets an object by its string based ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TModelObject> GetByIdAsync(string id = null);

        Task InsertAsync(TModelObject[] entities);

        Task UpdateBasicAsync(TModelObject[] entities);

        Task UpdateFullAsync(TModelObject[] entities);

        Task DeleteAsync(TModelObject[] entities);
    }
}