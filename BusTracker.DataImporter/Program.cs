using BusTracker.Core.Entities;
using BusTracker.DataImporter.DTO;
using BusTracker.Infrastructure.Data;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Globalization;

const int Srid = 4326;
const int ProgressLogInterval = 100000;
const int BatchSize = 5000;
const string DataDirectoryName = "Data";
const string StopsFileName = "stops.txt";
const string RoutesFileName = "routes.txt";
const string TripsFileName = "trips.txt";
const string StopTimesFileName = "stop_times.txt";
const string DefaultConnectionString = "Host=localhost;Database=bustrackerdb;Username=postgres;Password=123";

var baseDirectory = AppContext.BaseDirectory;
var dataDirectory = Path.Combine(baseDirectory, DataDirectoryName);

var stopsFilePath = Path.Combine(dataDirectory, StopsFileName);
var routesFilePath = Path.Combine(dataDirectory, RoutesFileName);
var tripsFilePath = Path.Combine(dataDirectory, TripsFileName);
var stopTimesFilePath = Path.Combine(dataDirectory, StopTimesFileName);

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? DefaultConnectionString;

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseNpgsql(connectionString, o => o.UseNetTopologySuite());

Console.WriteLine("=== GTFS Importer Started ===");

var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    Delimiter = ",",
    HasHeaderRecord = true,
    MissingFieldFound = null,
    HeaderValidated = null,
    BadDataFound = null
};

using (var context = new AppDbContext(optionsBuilder.Options))
{
    Console.WriteLine("1. Importing Stops...");
    await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"BusStops\" RESTART IDENTITY CASCADE");

    if (!File.Exists(stopsFilePath)) throw new FileNotFoundException($"{StopsFileName} not found", stopsFilePath);

    using var reader = new StreamReader(stopsFilePath);
    using var csv = new CsvReader(reader, csvConfig);

    var records = csv.GetRecords<StopGtfsDTO>();
    var geometryFactory = new GeometryFactory(new PrecisionModel(), Srid);
    var stopsToSave = new List<BusStop>();

    foreach (var record in records)
    {
        stopsToSave.Add(new BusStop
        {
            Code = record.StopId,
            Name = record.StopName,
            Location = geometryFactory.CreatePoint(new Coordinate(record.Longitude, record.Latitude))
        });
    }

    await context.BusStops.AddRangeAsync(stopsToSave);
    await context.SaveChangesAsync();
    Console.WriteLine($" -> {stopsToSave.Count} stops imported.");
}

using (var context = new AppDbContext(optionsBuilder.Options))
{
    Console.WriteLine("2. Importing Routes...");
    await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"BusLines\" RESTART IDENTITY CASCADE");

    if (!File.Exists(routesFilePath)) throw new FileNotFoundException($"{RoutesFileName} not found", routesFilePath);

    using var reader = new StreamReader(routesFilePath);
    using var csv = new CsvReader(reader, csvConfig);

    var records = csv.GetRecords<RouteGtfsDTO>();
    var linesToSave = new List<BusLine>();

    foreach (var r in records)
    {
        linesToSave.Add(new BusLine
        {
            ExternalId = r.RouteId,
            DisplayNumber = r.ShortName,
            Name = r.LongName
        });
    }

    await context.BusLines.AddRangeAsync(linesToSave);
    await context.SaveChangesAsync();
    Console.WriteLine($" -> {linesToSave.Count} lines imported.");
}

using (var context = new AppDbContext(optionsBuilder.Options))
{
    Console.WriteLine("3. Processing Relationships (This may take a while)...");
    await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"BusLineStops\" RESTART IDENTITY CASCADE");

    if (!File.Exists(tripsFilePath)) throw new FileNotFoundException($"{TripsFileName} not found", tripsFilePath);
    if (!File.Exists(stopTimesFilePath)) throw new FileNotFoundException($"{StopTimesFileName} not found", stopTimesFilePath);

    var routeMap = await context.BusLines.ToDictionaryAsync(x => x.ExternalId, x => x.Id);
    var stopMap = await context.BusStops.ToDictionaryAsync(x => x.Code, x => x.Id);

    Console.WriteLine("   -> Caching Trips...");
    var tripToRouteMap = new Dictionary<string, string>();

    using (var rTrips = new StreamReader(tripsFilePath))
    using (var csvTrips = new CsvReader(rTrips, csvConfig))
    {
        var trips = csvTrips.GetRecords<TripGtfsDTO>();
        foreach (var t in trips)
        {
            tripToRouteMap[t.TripId] = t.RouteId;
        }
    }
    Console.WriteLine($"   -> Cached {tripToRouteMap.Count} trips.");

    Console.WriteLine("   -> Streaming StopTimes and building unique connections...");

    var uniqueConnections = new HashSet<(int LineId, int StopId)>();

    using (var rStops = new StreamReader(stopTimesFilePath))
    using (var csvStops = new CsvReader(rStops, csvConfig))
    {
        var stopTimes = csvStops.GetRecords<StopTimeGtfsDTO>();

        int rowCount = 0;
        foreach (var st in stopTimes)
        {
            rowCount++;
            if (rowCount % ProgressLogInterval == 0) Console.Write(".");

            if (!tripToRouteMap.TryGetValue(st.TripId, out var routeGtfsId)) continue;
            if (!routeMap.TryGetValue(routeGtfsId, out var dbLineId)) continue;
            if (!stopMap.TryGetValue(st.StopId, out var dbStopId)) continue;

            uniqueConnections.Add((dbLineId, dbStopId));
        }
    }
    Console.WriteLine();
    Console.WriteLine($"   -> Found {uniqueConnections.Count} unique Line-Stop relationships.");

    Console.WriteLine("   -> Saving to database...");
    var batch = new List<BusLineStop>();
    int count = 0;

    foreach (var conn in uniqueConnections)
    {
        batch.Add(new BusLineStop
        {
            BusLineId = conn.LineId,
            BusStopId = conn.StopId,
            Sequence = 0
        });

        count++;
        if (count % BatchSize == 0)
        {
            await context.BusLineStops.AddRangeAsync(batch);
            await context.SaveChangesAsync();
            batch.Clear();
            Console.Write("+");
        }
    }

    if (batch.Any())
    {
        await context.BusLineStops.AddRangeAsync(batch);
        await context.SaveChangesAsync();
    }

    Console.WriteLine();
    Console.WriteLine("=== Import Complete Successfully ===");
}