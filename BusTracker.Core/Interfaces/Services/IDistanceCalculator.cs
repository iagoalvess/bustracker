namespace BusTracker.Core.Interfaces.Services
{
    /// <summary>
    /// Service interface for calculating distances between geographic coordinates.
    /// </summary>
    public interface IDistanceCalculator
    {
        /// <summary>
        /// Calculates the great-circle distance between two geographic points using the Haversine formula.
        /// </summary>
        /// <param name="lat1">Latitude of the first point.</param>
        /// <param name="lon1">Longitude of the first point.</param>
        /// <param name="lat2">Latitude of the second point.</param>
        /// <param name="lon2">Longitude of the second point.</param>
        /// <returns>Distance in meters.</returns>
        double CalculateDistanceInMeters(double lat1, double lon1, double lat2, double lon2);
    }
}
