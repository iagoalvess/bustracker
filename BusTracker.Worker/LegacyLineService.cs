namespace BusTracker.Worker;

/// <summary>
/// Service for loading and translating legacy internal line codes to display numbers.
/// </summary>
public class LegacyLineService
{
    private const int MinColumnsRequired = 2;
    private const int HeaderRowsToSkip = 1;

    private readonly ILogger<LegacyLineService> _logger;
    private Dictionary<string, string> _legacyMap = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyLineService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public LegacyLineService(ILogger<LegacyLineService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads the legacy line mapping from a CSV file.
    /// </summary>
    /// <param name="filePath">Path to the CSV file containing internal ID to display number mappings.</param>
    public void LoadMap(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Legacy mapping file not found: {Path}", filePath);
            return;
        }

        var newMap = new Dictionary<string, string>();
        
        try 
        {
            foreach (var line in File.ReadLines(filePath).Skip(HeaderRowsToSkip))
            {
                var cols = line.Split(';');
                if (cols.Length >= MinColumnsRequired)
                {
                    var internalId = cols[0].Trim();
                    var displayNumber = cols[1].Trim();
                    
                    newMap[internalId] = displayNumber;
                }
            }
            
            _legacyMap = newMap;
            _logger.LogInformation("Legacy map loaded with {Count} lines", _legacyMap.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading legacy file");
        }
    }

    /// <summary>
    /// Translates an internal line ID to its display number.
    /// </summary>
    /// <param name="internalId">The internal line identifier.</param>
    /// <returns>The display number if found, otherwise returns the original internal ID.</returns>
    public string GetDisplayNumber(string internalId)
    {
        return _legacyMap.TryGetValue(internalId, out var display) ? display : internalId;
    }
}
