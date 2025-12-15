namespace BusTracker.Core.Configuration
{
    /// <summary>
    /// Configuration settings for the bus tracker application.
    /// </summary>
    public class BusTrackerSettings
    {
        public const string SectionName = "BusTracker";

        /// <summary>
        /// Gets or sets the URL to fetch real-time bus data from PBH.
        /// </summary>
        public string PbhDataUrl { get; set; } = "https://temporeal.pbh.gov.br/?param=C";
        
        /// <summary>
        /// Gets or sets the interval in seconds between position updates.
        /// </summary>
        public int UpdateIntervalSeconds { get; set; } = 30;
        
        /// <summary>
        /// Gets or sets how long (in minutes) to retain position data before cleanup.
        /// </summary>
        public int PositionRetentionMinutes { get; set; } = 5;

        /// <summary>
        /// Gets or sets the time window in minutes for bus arrival predictions.
        /// </summary>
        public int PredictionTimeWindowMinutes { get; set; } = 5;

        /// <summary>
        /// Gets or sets the path to the legacy line mapping CSV file.
        /// </summary>
        public string LegacyLineMapPath { get; set; } = "bhtrans_bdlinha.csv";
    }
}
