using NetTopologySuite.Geometries;

namespace BusTracker.Core.Entities
{
    /// <summary>
    /// Represents a bus stop location.
    /// </summary>
    public class BusStop
    {
        /// <summary>
        /// Gets or sets the unique identifier for this bus stop.
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the stop code used for identification.
        /// </summary>
        public required string Code { get; set; }
        
        /// <summary>
        /// Gets or sets the name or description of the stop.
        /// </summary>
        public required string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the geographic location (Point with longitude and latitude).
        /// </summary>
        public required Point Location { get; set; }
    }
}