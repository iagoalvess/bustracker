using BusTracker.Core.Configuration;
using BusTracker.Core.Entities;
using BusTracker.Core.Interfaces;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using System.Globalization;

namespace BusTracker.Worker;

/// <summary>
/// Background service responsible for periodically fetching and updating bus positions.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly BusTrackerSettings _settings;

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IOptions<BusTrackerSettings> settings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
    }

    /// <summary>
    /// Executes the background service, continuously updating bus positions.
    /// </summary>
    /// <param name="stoppingToken">Token to monitor for cancellation requests.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        var brazilianCulture = new CultureInfo("pt-BR");

        _logger.LogInformation("Worker service started. Update interval: {Interval}s", _settings.UpdateIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBusPositionsAsync(geometryFactory, brazilianCulture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bus positions");
            }

            await Task.Delay(_settings.UpdateIntervalSeconds * 1000, stoppingToken);
        }
    }

    /// <summary>
    /// Processes bus position data from the external source and updates the database.
    /// </summary>
    /// <param name="geometryFactory">Factory for creating geographic points.</param>
    /// <param name="brazilianCulture">Culture info for parsing Brazilian number formats.</param>
    private async Task ProcessBusPositionsAsync(GeometryFactory geometryFactory, CultureInfo brazilianCulture)
    {
        _logger.LogInformation("Starting position update cycle");

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Dictionary<int, string> lineMap;
        try
        {
            var lines = await unitOfWork.BusLines.GetAllAsync();
            lineMap = lines.ToDictionary(k => k.ExternalId, v => v.DisplayNumber);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load line map, using empty dictionary");
            lineMap = new Dictionary<int, string>();
        }

        var retentionThreshold = DateTime.UtcNow.AddMinutes(-_settings.PositionRetentionMinutes);
        var oldPositions = await unitOfWork.BusPositions.FindAsync(x => x.Timestamp < retentionThreshold);

        if (oldPositions.Any())
        {
            unitOfWork.BusPositions.RemoveRange(oldPositions);
            await unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Cleaned {Count} old positions", oldPositions.Count());
        }

        var positions = new List<BusPosition>();
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "BusTrackerApp/1.0");
        client.Timeout = TimeSpan.FromSeconds(30);

        try
        {
            var csvContent = await client.GetStringAsync(_settings.PbhDataUrl);
            var csvLines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            _logger.LogDebug("Processing {Count} CSV lines", csvLines.Length);

            for (int i = 1; i < csvLines.Length; i++)
            {
                var columns = csvLines[i].Split(';');

                if (columns.Length < 7)
                {
                    _logger.LogTrace("Skipping line {Index}: insufficient columns", i);
                    continue;
                }

                try
                {
                    if (!int.TryParse(columns[6], out int lineCode))
                        continue;

                    string displayNumber = lineMap.TryGetValue(lineCode, out var mapped)
                        ? mapped
                        : lineCode.ToString();

                    if (!DateTime.TryParseExact(columns[1], "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timestamp))
                        continue;

                    if (!double.TryParse(columns[2], NumberStyles.Any, brazilianCulture, out double lat))
                        continue;

                    if (!double.TryParse(columns[3], NumberStyles.Any, brazilianCulture, out double lon))
                        continue;

                    string vehicleNumber = columns[4];

                    positions.Add(new BusPosition
                    {
                        LineNumber = displayNumber,
                        VehicleNumber = vehicleNumber,
                        Timestamp = timestamp.ToUniversalTime(),
                        Location = geometryFactory.CreatePoint(new Coordinate(lon, lat))
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogTrace(ex, "Error parsing line {Index}", i);
                    continue;
                }
            }

            if (positions.Any())
            {
                await unitOfWork.BusPositions.AddRangeAsync(positions);
                await unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully updated {Count} bus positions", positions.Count);
            }
            else
            {
                _logger.LogWarning("No valid positions found in CSV data");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error downloading data from PBH");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout downloading data from PBH");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing bus positions");
        }
    }
}