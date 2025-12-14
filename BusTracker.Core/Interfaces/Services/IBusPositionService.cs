using BusTracker.Core.Entities;

namespace BusTracker.Core.Interfaces.Services
{
    /// <summary>
    /// Service interface for managing bus position data.
    /// </summary>
    public interface IBusPositionService
    {
        /// <summary>
        /// Updates the database with new bus positions.
        /// </summary>
        /// <param name="positions">Collection of bus positions to save.</param>
        Task UpdatePositionsAsync(IEnumerable<BusPosition> positions);
        
        /// <summary>
        /// Removes old bus positions from the database.
        /// </summary>
        /// <param name="olderThan">Threshold date - positions older than this will be deleted.</param>
        Task CleanOldPositionsAsync(DateTime olderThan);
    }
}
