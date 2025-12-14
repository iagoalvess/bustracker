using BusTracker.Core.Entities;
using BusTracker.Core.Interfaces;
using BusTracker.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BusTracker.Infrastructure.Services
{
    /// <summary>
    /// Service for managing bus position data operations.
    /// </summary>
    public class BusPositionService : IBusPositionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BusPositionService> _logger;

        public BusPositionService(IUnitOfWork unitOfWork, ILogger<BusPositionService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Updates the database with new bus positions.
        /// </summary>
        /// <param name="positions">Collection of bus positions to save.</param>
        public async Task UpdatePositionsAsync(IEnumerable<BusPosition> positions)
        {
            var positionsList = positions.ToList();

            if (!positionsList.Any())
            {
                _logger.LogWarning("No positions to update.");
                return;
            }

            await _unitOfWork.BusPositions.AddRangeAsync(positionsList);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Successfully updated {Count} bus positions.", positionsList.Count);
        }

        /// <summary>
        /// Removes old bus positions from the database.
        /// </summary>
        /// <param name="olderThan">Threshold date - positions older than this will be deleted.</param>
        public async Task CleanOldPositionsAsync(DateTime olderThan)
        {
            var oldPositions = await _unitOfWork.BusPositions.FindAsync(x => x.Timestamp < olderThan);
            var oldPositionsList = oldPositions.ToList();

            if (oldPositionsList.Any())
            {
                _unitOfWork.BusPositions.RemoveRange(oldPositionsList);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Cleaned {Count} old bus positions.", oldPositionsList.Count);
            }
        }
    }
}
