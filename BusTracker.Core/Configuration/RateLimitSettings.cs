namespace BusTracker.Core.Configuration
{
    /// <summary>
    /// Configuration settings for API rate limiting.
    /// </summary>
    public class RateLimitSettings
    {
        public const string SectionName = "RateLimit";

        /// <summary>
        /// Gets or sets whether rate limiting is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the maximum number of requests allowed in the time window.
        /// </summary>
        public int PermitLimit { get; set; } = 20;
        
        /// <summary>
        /// Gets or sets the time window in seconds for rate limiting.
        /// </summary>
        public int WindowSeconds { get; set; } = 60;
        
        /// <summary>
        /// Gets or sets the maximum number of queued requests.
        /// </summary>
        public int QueueLimit { get; set; } = 10;
    }
}
