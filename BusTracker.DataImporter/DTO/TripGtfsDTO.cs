using CsvHelper.Configuration.Attributes;

namespace BusTracker.DataImporter.DTO
{
    /// <summary>
    /// DTO for importing trip data from GTFS format.
    /// </summary>
    public class TripGtfsDTO
    {
        /// <summary>
        /// Gets or sets the trip identifier.
        /// </summary>
        [Name("trip_id")]
        public required string TripId { get; set; }

        /// <summary>
        /// Gets or sets the route identifier this trip belongs to.
        /// </summary>
        [Name("route_id")]
        public required string RouteId { get; set; }

        /// <summary>
        /// Gets or sets the service identifier.
        /// </summary>
        [Name("service_id")]
        public required string ServiceId { get; set; }

        /// <summary>
        /// Gets or sets the trip headsign (destination display).
        /// </summary>
        [Name("trip_headsign")]
        public string? Headsign { get; set; }
    }
}