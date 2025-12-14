using System.Linq.Expressions;

namespace BusTracker.Core.Interfaces
{
    /// <summary>
    /// Generic repository interface for data access operations.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Gets an entity by its identifier.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <returns>The entity if found, otherwise null.</returns>
        Task<T?> GetByIdAsync(int id);
        
        /// <summary>
        /// Gets all entities of this type.
        /// </summary>
        /// <returns>A collection of all entities.</returns>
        Task<IEnumerable<T>> GetAllAsync();
        
        /// <summary>
        /// Finds entities matching the specified predicate.
        /// </summary>
        /// <param name="predicate">The filter expression.</param>
        /// <returns>A collection of matching entities.</returns>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        
        /// <summary>
        /// Gets the first entity matching the predicate or null.
        /// </summary>
        /// <param name="predicate">The filter expression.</param>
        /// <returns>The first matching entity or null.</returns>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        
        /// <summary>
        /// Adds a new entity to the repository.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        Task AddAsync(T entity);
        
        /// <summary>
        /// Adds multiple entities to the repository.
        /// </summary>
        /// <param name="entities">The entities to add.</param>
        Task AddRangeAsync(IEnumerable<T> entities);
        
        /// <summary>
        /// Updates an existing entity.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        void Update(T entity);
        
        /// <summary>
        /// Removes an entity from the repository.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        void Remove(T entity);
        
        /// <summary>
        /// Removes multiple entities from the repository.
        /// </summary>
        /// <param name="entities">The entities to remove.</param>
        void RemoveRange(IEnumerable<T> entities);
    }
}
