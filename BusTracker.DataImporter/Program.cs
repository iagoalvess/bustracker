using BusTracker.Core.Entities;
using BusTracker.DataImporter.DTO;
using BusTracker.Infrastructure.Data;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Globalization;
using System.Text;

var baseDirectory = AppContext.BaseDirectory;
var dataDirectory = Path.Combine(baseDirectory, "Data");

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? "Host=localhost;Database=BusTrackerDb;Username=postgres;Password=123";

var stopsFilePath = Environment.GetEnvironmentVariable("DataImport__StopsFile")
    ?? Path.Combine(dataDirectory, "stops.txt");

var linesFilePath = Environment.GetEnvironmentVariable("DataImport__LinesFile")
    ?? Path.Combine(dataDirectory, "bhtrans_bdlinha.csv");

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseNpgsql(connectionString, o => o.UseNetTopologySuite());

Console.WriteLine("=== Bus Stops Importer ===");

using (var context = new AppDbContext(optionsBuilder.Options))
{
    Console.WriteLine("Clearing old data");
    await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"BusStops\" RESTART IDENTITY CASCADE");

    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        Delimiter = ",",
        HasHeaderRecord = true,
        MissingFieldFound = null,
        HeaderValidated = null
    };

    using (var reader = new StreamReader(stopsFilePath))
    using (var csv = new CsvReader(reader, config))
    {
        var records = csv.GetRecords<StopGtfsDTO>();
        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);

        var stopsToSave = new List<BusStop>();

        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.StopName)) continue;

            var newStop = new BusStop
            {
                Code = record.StopId,
                Name = record.StopName,
                Location = geometryFactory.CreatePoint(new Coordinate(record.Longitude, record.Latitude))
            };

            stopsToSave.Add(newStop);
        }

        Console.WriteLine($"{stopsToSave.Count} stops inserted.");

        await context.BusStops.AddRangeAsync(stopsToSave);
        await context.SaveChangesAsync();
    }
}

Console.WriteLine("=== Bus Lines Importer ===");

using (var context = new AppDbContext(optionsBuilder.Options))
{
    Console.WriteLine("Clearing old data");
    await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"BusLines\" RESTART IDENTITY CASCADE");

    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        Delimiter = ";",
        HasHeaderRecord = true,
    };

    using (var reader = new StreamReader(linesFilePath, Encoding.UTF8))
    using (var csv = new CsvReader(reader, config))
    {
        var records = csv.GetRecords<BusLineCsvDTO>();
        var linesToSave = new List<BusLine>();

        Console.WriteLine("Reading lines...");
        foreach (var record in records)
        {
            linesToSave.Add(new BusLine
            {
                ExternalId = record.LineNumber,
                DisplayNumber = record.Line,
                Name = record.Name
            });
        }

        Console.WriteLine($"{linesToSave.Count} lines inserted.");
        await context.BusLines.AddRangeAsync(linesToSave);
        await context.SaveChangesAsync();
    }
}

