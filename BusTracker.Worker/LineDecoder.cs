namespace BusTracker.Worker
{
    /// <summary>
    /// Utility class for decoding and mapping bus line internal codes to display names.
    /// </summary>
    public class LineDecoder
    {
        private readonly Dictionary<int, string> _lineMap = new();

        /// <summary>
        /// Loads the line mapping from a CSV file.
        /// </summary>
        /// <param name="filePath">Path to the CSV file containing line mappings.</param>
        public void LoadMap(string filePath)
        {
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines.Skip(1))
            {
                var columns = line.Split(';');
                if (columns.Length >= 2)
                {
                    if (int.TryParse(columns[0], out int internalCode))
                    {
                        var displayName = columns[1];
                        _lineMap[internalCode] = displayName;
                    }
                }
            }
            Console.WriteLine($"Line map loaded! {_lineMap.Count} lines known.");
        }

        /// <summary>
        /// Gets the display name for a given internal line code.
        /// </summary>
        /// <param name="internalCode">The internal line code.</param>
        /// <returns>The display name or "UNKNOWN" if not found.</returns>
        public string GetRealLineNumber(int internalCode)
        {
            if (_lineMap.TryGetValue(internalCode, out var displayName))
                return displayName;

            return "UNKNOWN";
        }
    }
}
