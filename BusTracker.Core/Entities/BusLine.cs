namespace BusTracker.Core.Entities
{
    /// <summary>
    /// Represents a bus line with its identification and display information.
    /// </summary>
    public class BusLine
    {
        /// <summary>
        /// Gets or sets the unique identifier for this bus line.
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the external system identifier for this line (GTFS route_id).
        /// </summary>
        public required string ExternalId { get; set; }
        
        /// <summary>
        /// Gets or sets the line number displayed to users.
        /// </summary>
        public required string DisplayNumber { get; set; }
        
        /// <summary>
        /// Gets or sets the line name or route description.
        /// </summary>
        public required string Name { get; set; }
    }
}
