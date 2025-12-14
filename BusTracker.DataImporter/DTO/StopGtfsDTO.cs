namespace BusTracker.DataImporter.DTO
{
    /// <summary>
    /// DTO for importing bus stop data from GTFS format.
    /// </summary>
    public class StopGtfsDTO
    {
        /// <summary>
        /// Gets or sets the unique stop identifier.
        /// </summary>
        [CsvHelper.Configuration.Attributes.Name("stop_id")]
        public required string StopId { get; set; }

        /// <summary>
        /// Gets or sets the stop name.
        /// </summary>
        [CsvHelper.Configuration.Attributes.Name("stop_name")]
        public required string StopName { get; set; }

        /// <summary>
        /// Gets or sets the latitude coordinate.
        /// </summary>
        [CsvHelper.Configuration.Attributes.Name("stop_lat")]
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate.
        /// </summary>
        [CsvHelper.Configuration.Attributes.Name("stop_lon")]
        public double Longitude { get; set; }
    }
}
