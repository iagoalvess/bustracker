using BusTracker.Core.Interfaces.Services;

namespace BusTracker.Infrastructure.Services
{
    /// <summary>
    /// Service for calculating distances between geographic coordinates using the Haversine formula.
    /// </summary>
    public class DistanceCalculator : IDistanceCalculator
    {
        private const double EarthRadiusInMeters = 6371e3;

        /// <summary>
        /// Calculates the great-circle distance between two geographic points.
        /// </summary>
        /// <param name="lat1">Latitude of the first point.</param>
        /// <param name="lon1">Longitude of the first point.</param>
        /// <param name="lat2">Latitude of the second point.</param>
        /// <param name="lon2">Longitude of the second point.</param>
        /// <returns>Distance in meters.</returns>
        public double CalculateDistanceInMeters(double lat1, double lon1, double lat2, double lon2)
        {
            var rad = Math.PI / 180;
            var dLat = (lat2 - lat1) * rad;
            var dLon = (lon2 - lon1) * rad;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * rad) * Math.Cos(lat2 * rad) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusInMeters * c;
        }
    }
}
