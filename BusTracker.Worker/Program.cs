using BusTracker.Core.Configuration;
using BusTracker.Core.Interfaces;
using BusTracker.Infrastructure.Data;
using BusTracker.Infrastructure.Repositories;
using BusTracker.Worker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<BusTrackerSettings>(builder.Configuration.GetSection(BusTrackerSettings.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")!,
    o => o.UseNetTopologySuite()));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<LegacyLineService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();