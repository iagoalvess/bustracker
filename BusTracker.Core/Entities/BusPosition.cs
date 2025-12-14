using NetTopologySuite.Geometries;

namespace BusTracker.Core.Entities
{
    /// <summary>
    /// Represents a real-time position of a bus vehicle.
    /// </summary>
    public class BusPosition
    {
        /// <summary>
        /// Gets or sets the unique identifier for this position record.
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the UTC timestamp when this position was recorded.
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Gets or sets the bus line number.
        /// </summary>
        public required string LineNumber { get; set; }
        
        /// <summary>
        /// Gets or sets the vehicle identifier.
        /// </summary>
        public required string VehicleNumber { get; set; }
        
        /// <summary>
        /// Gets or sets the geographic location (Point with longitude and latitude).
        /// </summary>
        public required Point Location { get; set; }
    }
}