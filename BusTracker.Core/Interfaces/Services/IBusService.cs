using BusTracker.Core.DTOs;

namespace BusTracker.Core.Interfaces.Services
{
    /// <summary>
    /// Service interface for bus-related operations including searching stops, lines, and predictions.
    /// </summary>
    public interface IBusService
    {
        /// <summary>
        /// Searches for bus stops matching the provided query.
        /// </summary>
        /// <param name="query">Search term (minimum 3 characters).</param>
        /// <returns>A collection of matching bus stops.</returns>
        Task<IEnumerable<BusStopResponseDto>> SearchStopsAsync(string query);
        
        /// <summary>
        /// Searches for bus lines matching the provided query.
        /// </summary>
        /// <param name="query">Search term for line number or name.</param>
        /// <returns>A collection of matching bus lines.</returns>
        Task<IEnumerable<BusLineResponseDto>> SearchLinesAsync(string query);
        
        /// <summary>
        /// Gets arrival prediction for a specific bus line at a stop.
        /// Now returns also the second closest bus when available.
        /// </summary>
        /// <param name="stopCode">The bus stop code.</param>
        /// <param name="lineNumber">The bus line number.</param>
        /// <returns>Prediction information with distance and estimated time, including second closest bus.</returns>
        Task<BusPredictionResponseDto> GetPredictionAsync(string stopCode, string lineNumber);
    }
}
