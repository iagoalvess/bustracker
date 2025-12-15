using BusTracker.Core.Entities;

namespace BusTracker.Core.Interfaces
{
    /// <summary>
    /// Unit of Work pattern interface for coordinating repository operations and transactions.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Gets the repository for bus stops.
        /// </summary>
        IRepository<BusStop> BusStops { get; }
        
        /// <summary>
        /// Gets the repository for bus positions.
        /// </summary>
        IRepository<BusPosition> BusPositions { get; }
        
        /// <summary>
        /// Gets the repository for bus lines.
        /// </summary>
        IRepository<BusLine> BusLines { get; }
        
        /// <summary>
        /// Gets the repository for bus line stops (line-stop relationships).
        /// </summary>
        IRepository<BusLineStop> BusLineStops { get; }
        
        /// <summary>
        /// Saves all pending changes to the database.
        /// </summary>
        /// <returns>The number of affected records.</returns>
        Task<int> SaveChangesAsync();
        
        /// <summary>
        /// Executes a bulk delete operation on the provided query.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="query">The query defining entities to delete.</param>
        /// <returns>The number of deleted records.</returns>
        Task<int> ExecuteDeleteAsync<T>(IQueryable<T> query) where T : class;
    }
}
