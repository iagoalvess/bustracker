namespace BusTracker.Core.DTOs
{
    /// <summary>
    /// Response DTO for stops served by a specific line.
    /// </summary>
    public class StopOnLineResponseDto
    {
        /// <summary>
        /// Gets or sets the stop code.
        /// </summary>
        public required string StopCode { get; set; }
        
        /// <summary>
        /// Gets or sets the stop name.
        /// </summary>
        public required string StopName { get; set; }
        
        /// <summary>
        /// Gets or sets the sequence order of this stop on the route.
        /// </summary>
        public int Sequence { get; set; }
        
        /// <summary>
        /// Gets or sets the longitude coordinate.
        /// </summary>
        public double Longitude { get; set; }
        
        /// <summary>
        /// Gets or sets the latitude coordinate.
        /// </summary>
        public double Latitude { get; set; }
    }
}
