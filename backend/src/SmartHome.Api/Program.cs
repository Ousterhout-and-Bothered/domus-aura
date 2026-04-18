using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Device.Repository;
using SmartHome.Infrastructure.Device.Repository;
using SmartHome.Infrastructure.Persistence;
using SmartHome.Infrastructure.Persistence.Seed;
using SmartHome.Domain.Device.Registration;
using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Device.Fan;
using SmartHome.Domain.Device.DoorLock;
using SmartHome.Domain.Device.Thermostat;
using SmartHome.Infrastructure.Simulation;

// Create the application builder and load configuration/services
var builder = WebApplication.CreateBuilder(args);

// Register OpenAPI support
builder.Services.AddOpenApi();

// Register MVC controllers
builder.Services.AddControllers();

// Resolve the shared data directory relative to the API project
var dataDirectory = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "data"));

// Ensure the data directory exists before using SQLite
Directory.CreateDirectory(dataDirectory);

// Build the full SQLite database path
var databasePath = Path.Combine(dataDirectory, "smarthome.db");

// Register EF Core DbContext using SQLite persistence
builder.Services.AddDbContext<SmartHomeDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

// Register persistence and seeding services
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<SmartHomeDbSeeder>();
builder.Services.AddScoped<IDeviceFactory, DeviceFactory>();

// Register all device builders used by the factory
builder.Services.AddScoped<IDeviceBuilder, LightBuilder>();
builder.Services.AddScoped<IDeviceBuilder, FanBuilder>();
builder.Services.AddScoped<IDeviceBuilder, DoorLockBuilder>();
builder.Services.AddScoped<IDeviceBuilder, ThermostatBuilder>();

// Register simulation service
builder.Services.AddSingleton<ISimulationService, SimulationService>();

// Runs the background loop that updates thermostat behavior over time
builder.Services.AddHostedService<ThermostatSimulationBackgroundService>();

// Build the application pipeline
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Expose OpenAPI document in development
    app.MapOpenApi();
}

// Redirect HTTP requests to HTTPS
app.UseHttpsRedirection();

// Log resolved database path for debugging startup issues
app.Logger.LogDebug("DB Path: {DatabasePath}", databasePath);

using (var scope = app.Services.CreateScope())
{
    // Resolve DbContext for startup migration
    var dbContext = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

    // Apply any pending EF Core migrations automatically on startup
    await dbContext.Database.MigrateAsync();

    // Seed initial smart home data if database is empty
    var seeder = scope.ServiceProvider.GetRequiredService<SmartHomeDbSeeder>();
    await seeder.SeedAsync();
}

// Map attribute-routed API controllers
app.MapControllers();

// Start the web application
app.Run();