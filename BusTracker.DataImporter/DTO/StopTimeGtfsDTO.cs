using CsvHelper.Configuration.Attributes;

namespace BusTracker.DataImporter.DTO
{
    /// <summary>
    /// DTO for importing stop time data from GTFS format.
    /// </summary>
    public class StopTimeGtfsDTO
    {
        /// <summary>
        /// Gets or sets the trip identifier.
        /// </summary>
        [Name("trip_id")]
        public required string TripId { get; set; }

        /// <summary>
        /// Gets or sets the stop identifier.
        /// </summary>
        [Name("stop_id")]
        public required string StopId { get; set; }

        /// <summary>
        /// Gets or sets the sequence order of this stop in the trip.
        /// </summary>
        [Name("stop_sequence")]
        public int Sequence { get; set; }
    }
}