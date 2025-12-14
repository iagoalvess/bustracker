namespace BusTracker.DataImporter.DTO
{
    /// <summary>
    /// DTO for importing bus line data from CSV format.
    /// </summary>
    public class BusLineCsvDTO
    {
        /// <summary>
        /// Gets or sets the internal line number identifier.
        /// </summary>
        [CsvHelper.Configuration.Attributes.Name("NumeroLinha")]
        public int LineNumber { get; set; }
        
        /// <summary>
        /// Gets or sets the line display number shown to users.
        /// </summary>
        [CsvHelper.Configuration.Attributes.Name("Linha")]
        public required string Line { get; set; }
        
        /// <summary>
        /// Gets or sets the line name or route description.
        /// </summary>
        [CsvHelper.Configuration.Attributes.Name("Nome")]
        public required string Name { get; set; }
    }
}
