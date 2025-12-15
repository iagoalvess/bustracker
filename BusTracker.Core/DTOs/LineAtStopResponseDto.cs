namespace BusTracker.Core.DTOs
{
    /// <summary>
    /// Response DTO for lines serving a specific stop.
    /// </summary>
    public class LineAtStopResponseDto
    {
        /// <summary>
        /// Gets or sets the line number.
        /// </summary>
        public required string LineNumber { get; set; }
        
        /// <summary>
        /// Gets or sets the line description/name.
        /// </summary>
        public required string LineName { get; set; }
        
        /// <summary>
        /// Gets or sets the sub-line variant if applicable.
        /// </summary>
        public string? SubLineName { get; set; }
    }
}
