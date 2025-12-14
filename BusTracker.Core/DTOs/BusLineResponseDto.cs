namespace BusTracker.Core.DTOs
{
    /// <summary>
    /// Represents a bus line in the response.
    /// </summary>
    public class BusLineResponseDto
    {
        /// <summary>
        /// Gets or sets the line identifier.
        /// </summary>
        /// <example>1</example>
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the line number displayed to users.
        /// </summary>
        /// <example>5101</example>
        public required string Number { get; set; }
        
        /// <summary>
        /// Gets or sets the line description or route name.
        /// </summary>
        /// <example>Central - Shopping</example>
        public required string Description { get; set; }
    }
}
