namespace BusTracker.Core.Entities
{
    /// <summary>
    /// Represents the relationship between a bus line and a bus stop.
    /// Indicates which stops are served by each line.
    /// </summary>
    public class BusLineStop
    {
        /// <summary>
        /// Gets or sets the unique identifier for this relationship.
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the bus line identifier.
        /// </summary>
        public int BusLineId { get; set; }
        
        /// <summary>
        /// Gets or sets the bus stop identifier.
        /// </summary>
        public int BusStopId { get; set; }
        
        /// <summary>
        /// Gets or sets the sequence/order of this stop in the route.
        /// </summary>
        public int Sequence { get; set; }
        
        /// <summary>
        /// Gets or sets the sub-line or variant name (e.g., "VIA SAVASSI").
        /// </summary>
        public string? SubLineName { get; set; }
        
        /// <summary>
        /// Navigation property to the bus line.
        /// </summary>
        public BusLine BusLine { get; set; } = null!;
        
        /// <summary>
        /// Navigation property to the bus stop.
        /// </summary>
        public BusStop BusStop { get; set; } = null!;
    }
}
