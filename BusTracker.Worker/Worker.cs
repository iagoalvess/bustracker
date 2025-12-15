using BusTracker.Core.Configuration;
using BusTracker.Core.Entities;
using BusTracker.Core.Interfaces;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using System.Globalization;

namespace BusTracker.Worker;

/// <summary>
/// Background service that continuously fetches and processes real-time bus position data.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly BusTrackerSettings _settings;
    private readonly LegacyLineService _legacyLineService;

    private Dictionary<string, string> _cachedLineMap = new();
    private DateTime _lastCacheUpdate = DateTime.MinValue;

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IOptions<BusTrackerSettings> settings,
        LegacyLineService legacyLineService)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _legacyLineService = legacyLineService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        var culture = CultureInfo.InvariantCulture;

        _logger.LogInformation("Worker started. Update interval: {Interval}s", _settings.UpdateIntervalSeconds);

        var legacyFilePath = Path.IsPathRooted(_settings.LegacyLineMapPath)
            ? _settings.LegacyLineMapPath
            : Path.Combine(AppContext.BaseDirectory, _settings.LegacyLineMapPath);
        _legacyLineService.LoadMap(legacyFilePath);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateLineCacheAsync();
                await ProcessBusPositionsAsync(geometryFactory, culture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in Worker cycle");
            }

            await Task.Delay(_settings.UpdateIntervalSeconds * 1000, stoppingToken);
        }
    }

    /// <summary>
    /// Updates the line cache from the database periodically (every hour).
    /// Maps both GTFS external IDs and display numbers to display numbers for flexible matching.
    /// </summary>
    private async Task UpdateLineCacheAsync()
    {
        if (_cachedLineMap.Any() && (DateTime.UtcNow - _lastCacheUpdate).TotalHours < 1)
            return;

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var lines = await unitOfWork.BusLines.GetAllAsync();

            _cachedLineMap = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                if (!string.IsNullOrEmpty(line.ExternalId))
                    _cachedLineMap[line.ExternalId] = line.DisplayNumber;

                if (!string.IsNullOrEmpty(line.DisplayNumber))
                    _cachedLineMap[line.DisplayNumber] = line.DisplayNumber;
            }

            _lastCacheUpdate = DateTime.UtcNow;
            _logger.LogInformation("Line cache updated: {Count} keys mapped", _cachedLineMap.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update line cache");
        }
    }

    /// <summary>
    /// Fetches bus positions from the CSV endpoint, parses them, and saves to the database.
    /// Includes intelligent cleanup of old positions and legacy line number translation.
    /// </summary>
    private async Task ProcessBusPositionsAsync(GeometryFactory geometryFactory, CultureInfo culture)
    {
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        client.Timeout = TimeSpan.FromSeconds(30);

        try
        {
            var csvContent = await client.GetStringAsync(_settings.PbhDataUrl);
            var csvLines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (csvLines.Length <= 1)
            {
                _logger.LogWarning("CSV file empty or contains only header");
                return;
            }

            _logger.LogInformation("CSV sample line: {Line}", csvLines[1]);

            var positions = new List<BusPosition>();
            int parseFailures = 0;

            for (int i = 1; i < csvLines.Length; i++)
            {
                var columns = csvLines[i].Split(';');

                if (columns.Length < 7)
                {
                    parseFailures++;
                    continue;
                }

                try
                {
                    if (!DateTime.TryParseExact(columns[1], "yyyyMMddHHmmss", culture, DateTimeStyles.None, out DateTime timestamp))
                    {
                        if (!DateTime.TryParse(columns[1], out timestamp))
                        {
                            if (parseFailures == 0) _logger.LogWarning("Date parsing error on line {Idx}: Value '{Val}'", i, columns[1]);
                            parseFailures++;
                            continue;
                        }
                    }

                    var latStr = columns[2].Replace(',', '.');
                    var lonStr = columns[3].Replace(',', '.');

                    if (!double.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) ||
                        !double.TryParse(lonStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                    {
                        parseFailures++;
                        continue;
                    }

                    var rawLineCode = columns[6].Trim();
                    var translatedLineCode = _legacyLineService.GetDisplayNumber(rawLineCode);
                    string finalDisplayNumber = _cachedLineMap.TryGetValue(translatedLineCode, out var mappedName)
                        ? mappedName
                        : translatedLineCode;

                    var pos = new BusPosition
                    {
                        LineNumber = finalDisplayNumber,
                        VehicleNumber = columns[4],
                        Timestamp = timestamp.ToUniversalTime(),
                        Location = geometryFactory.CreatePoint(new Coordinate(lon, lat))
                    };

                    positions.Add(pos);
                }
                catch (Exception)
                {
                    parseFailures++;
                }
            }

            if (parseFailures > 0)
                _logger.LogWarning("Failed to process {Count} CSV lines (invalid format)", parseFailures);

            if (positions.Any())
            {
                using var scope = _scopeFactory.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var retentionThreshold = DateTime.UtcNow.AddMinutes(-_settings.PositionRetentionMinutes);
                var oldPositions = await unitOfWork.BusPositions.FindAsync(x => x.Timestamp < retentionThreshold);
                if (oldPositions.Any())
                {
                    unitOfWork.BusPositions.RemoveRange(oldPositions);
                }

                await unitOfWork.BusPositions.AddRangeAsync(positions);
                await unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Success: {Count} bus positions updated", positions.Count);
            }
            else
            {
                _logger.LogWarning("No valid positions found to save");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error accessing PBH: {Msg}", ex.Message);
        }
    }
}