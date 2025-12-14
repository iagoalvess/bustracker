using BusTracker.API.Middleware;
using BusTracker.Core.Configuration;
using BusTracker.Core.Interfaces;
using BusTracker.Core.Interfaces.Services;
using BusTracker.Infrastructure.Data;
using BusTracker.Infrastructure.Repositories;
using BusTracker.Infrastructure.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<BusTrackerSettings>(builder.Configuration.GetSection(BusTrackerSettings.SectionName));
builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection(RateLimitSettings.SectionName));

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
    x => x.UseNetTopologySuite()));

// Repository Pattern
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<IBusService, BusService>();
builder.Services.AddSingleton<IDistanceCalculator, DistanceCalculator>();

// Controllers
builder.Services.AddControllers();

// API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "BusTracker API",
        Version = "v1",
        Description = @"## Real-Time Bus Tracking API"
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Health Checks
builder.Services.AddHealthChecks();

// Rate Limiting
var rateLimitSettings = builder.Configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>() ?? new RateLimitSettings();

if (rateLimitSettings.Enabled)
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Fixed Window Rate Limiter for general API
        options.AddFixedWindowLimiter("fixed", opt =>
        {
            opt.PermitLimit = rateLimitSettings.PermitLimit;
            opt.Window = TimeSpan.FromSeconds(rateLimitSettings.WindowSeconds);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = rateLimitSettings.QueueLimit;
        });

        // Sliding Window for prediction endpoint (more strict)
        options.AddSlidingWindowLimiter("prediction", opt =>
        {
            opt.PermitLimit = 30;
            opt.Window = TimeSpan.FromSeconds(60);
            opt.SegmentsPerWindow = 6;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 5;
        });

        // Concurrency limiter for search endpoints
        options.AddConcurrencyLimiter("search", opt =>
        {
            opt.PermitLimit = 50;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 10;
        });
    });
}

var app = builder.Build();

// Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BusTracker API v1");
        c.DocumentTitle = "BusTracker API - Documentation";
        c.DefaultModelsExpandDepth(2);
        c.DefaultModelExpandDepth(2);
        c.DisplayRequestDuration();
        c.EnableTryItOutByDefault();
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

if (rateLimitSettings.Enabled)
{
    app.UseRateLimiter();
}

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();