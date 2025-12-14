namespace BusTracker.Core.DTOs
{
    /// <summary>
    /// Represents the prediction for bus arrival at a stop.
    /// </summary>
    public class BusPredictionResponseDto
    {
        /// <summary>
        /// Gets or sets the bus line number.
        /// </summary>
        /// <example>5101</example>
        public required string Line { get; set; }
        
        /// <summary>
        /// Gets or sets the vehicle identifier.
        /// </summary>
        /// <example>BH-1234</example>
        public required string Vehicle { get; set; }
        
        /// <summary>
        /// Gets or sets the estimated distance to the stop in meters.
        /// </summary>
        /// <example>1250.5</example>
        public double DistanceInMeters { get; set; }
        
        /// <summary>
        /// Gets or sets the estimated time to arrival in minutes.
        /// </summary>
        /// <example>8.5</example>
        public double TimeInMinutes { get; set; }

        /// <summary>
        /// Gets or sets the second closest bus prediction, when available.
        /// </summary>
        public BusPredictionResponseDto? SecondClosest { get; set; }
    }
}
