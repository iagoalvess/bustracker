using BusTracker.Core.Interfaces.Services;

namespace BusTracker.Infrastructure.Services
{
    /// <summary>
    /// Service for calculating distances between geographic coordinates using the Haversine formula.
    /// </summary>
    public class DistanceCalculator : IDistanceCalculator
    {
        private const double EarthRadiusInMeters = 6371e3;
        private const double DegreesToRadians = Math.PI / 180;

        /// <inheritdoc />
        public double CalculateDistanceInMeters(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = (lat2 - lat1) * DegreesToRadians;
            var dLon = (lon2 - lon1) * DegreesToRadians;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * DegreesToRadians) * Math.Cos(lat2 * DegreesToRadians) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusInMeters * c;
        }
    }
}
