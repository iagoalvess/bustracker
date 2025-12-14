namespace BusTracker.Core.Interfaces.Services
{
    public interface IDistanceCalculator
    {
        double CalculateDistanceInMeters(double lat1, double lon1, double lat2, double lon2);
    }
}
