namespace BusTracker.Core.DTOs
{
    /// <summary>
    /// Represents a bus stop in the response.
    /// </summary>
    public class BusStopResponseDto
    {
        /// <summary>
        /// Gets or sets the unique stop code.
        /// </summary>
        /// <example>12345</example>
        public required string Code { get; set; }
        
        /// <summary>
        /// Gets or sets the stop name or location description.
        /// </summary>
        /// <example>Central Station - Main Entrance</example>
        public required string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the longitude coordinate.
        /// </summary>
        /// <example>-43.9378</example>
        public double Longitude { get; set; }
        
        /// <summary>
        /// Gets or sets the latitude coordinate.
        /// </summary>
        /// <example>-19.9167</example>
        public double Latitude { get; set; }
    }
}
