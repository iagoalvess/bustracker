namespace BusTracker.API.Models
{
    /// <summary>
    /// Standard error response model.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Error message describing what went wrong.
        /// </summary>
        /// <example>Please enter at least 3 characters.</example>
        public required string Error { get; set; }
    }
}
