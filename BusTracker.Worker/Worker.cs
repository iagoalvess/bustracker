using BusTracker.Core.Configuration;
using BusTracker.Core.Entities;
using BusTracker.Core.Interfaces;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using System.Globalization;

namespace BusTracker.Worker;

/// <summary>
/// Background service that periodically fetches real-time bus position data from PBH API
/// and stores it in the database for prediction calculations.
/// </summary>
public class Worker : BackgroundService
{
    private const int Srid = 4326;
    private const int HttpTimeoutSeconds = 30;
    private const int CacheRefreshIntervalHours = 1;
    private const int MinCsvColumns = 7;
    private const int BrazilUtcOffsetHours = -3;
    private const int MillisecondsPerSecond = 1000;
    private const string TimestampFormat = "yyyyMMddHHmmss";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly BusTrackerSettings _settings;
    private readonly LegacyLineService _legacyLineService;

    private Dictionary<string, string> _cachedLineMap = [];
    private DateTime _lastCacheUpdate = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="scopeFactory">The service scope factory for creating scoped services.</param>
    /// <param name="httpClientFactory">The HTTP client factory for making API requests.</param>
    /// <param name="settings">The bus tracker configuration settings.</param>
    /// <param name="legacyLineService">The service for translating legacy line codes.</param>
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

    /// <summary>
    /// Executes the background service, continuously fetching and storing bus positions.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token to stop the service.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var geometryFactory = new GeometryFactory(new PrecisionModel(), Srid);
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

            await Task.Delay(_settings.UpdateIntervalSeconds * MillisecondsPerSecond, stoppingToken);
        }
    }

    /// <summary>
    /// Updates the cached line mapping from the database.
    /// Cache is refreshed every hour.
    /// </summary>
    private async Task UpdateLineCacheAsync()
    {
        if (_cachedLineMap.Any() && (DateTime.UtcNow - _lastCacheUpdate).TotalHours < CacheRefreshIntervalHours)
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
    /// Fetches bus positions from the PBH API and stores them in the database.
    /// Also performs cleanup of old position records.
    /// </summary>
    /// <param name="geometryFactory">The geometry factory for creating location points.</param>
    /// <param name="culture">The culture info for parsing numeric values.</param>
    private async Task ProcessBusPositionsAsync(GeometryFactory geometryFactory, CultureInfo culture)
    {
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        client.Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds);

        try
        {
            var csvContent = await client.GetStringAsync(_settings.PbhDataUrl);
            var csvLines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (csvLines.Length <= 1) return;

            var positions = new List<BusPosition>();
            int parseFailures = 0;
            var brazilOffset = TimeSpan.FromHours(BrazilUtcOffsetHours);

            for (int i = 1; i < csvLines.Length; i++)
            {
                var columns = csvLines[i].Split(';');
                if (columns.Length < MinCsvColumns) continue;

                try
                {
                    if (!DateTime.TryParseExact(columns[1], TimestampFormat, culture, DateTimeStyles.None, out DateTime timestamp))
                    {
                        if (!DateTime.TryParse(columns[1], out timestamp))
                        {
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


                    var timestampInUtc = new DateTimeOffset(timestamp, brazilOffset).UtcDateTime;

                    var pos = new BusPosition
                    {
                        LineNumber = finalDisplayNumber,
                        VehicleNumber = columns[4],
                        Timestamp = timestampInUtc,
                        Location = geometryFactory.CreatePoint(new Coordinate(lon, lat))
                    };

                    positions.Add(pos);
                }
                catch (Exception)
                {
                    parseFailures++;
                }
            }

            if (positions.Count != 0)
            {
                using var scope = _scopeFactory.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                await unitOfWork.BusPositions.AddRangeAsync(positions);
                await unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Success: {Count} bus positions updated", positions.Count);

                try
                {
                    var retentionThreshold = DateTime.UtcNow.AddMinutes(-_settings.PositionRetentionMinutes);
                    var query = unitOfWork.BusPositions.GetQueryable()
                        .Where(x => x.Timestamp < retentionThreshold);

                    var deletedCount = await unitOfWork.ExecuteDeleteAsync(query);

                    if (deletedCount > 0)
                    {
                        _logger.LogInformation("Cleanup: Removed {Count} old positions", deletedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Minor cleanup warning (Ignored): {Msg}", ex.Message);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error accessing PBH: {Msg}", ex.Message);
        }
    }
}