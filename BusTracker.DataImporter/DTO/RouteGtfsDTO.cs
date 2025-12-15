using CsvHelper.Configuration.Attributes;

namespace BusTracker.DataImporter.DTO
{
    /// <summary>
    /// DTO for importing route data from GTFS format.
    /// </summary>
    public class RouteGtfsDTO
    {
        /// <summary>
        /// Gets or sets the route identifier.
        /// </summary>
        [Name("route_id")]
        public required string RouteId { get; set; }

        /// <summary>
        /// Gets or sets the route short name (e.g., "326").
        /// </summary>
        [Name("route_short_name")]
        public required string ShortName { get; set; }

        /// <summary>
        /// Gets or sets the route long name (e.g., "Station Barreiro / Vale do Jatoba").
        /// </summary>
        [Name("route_long_name")]
        public required string LongName { get; set; }
    }
}