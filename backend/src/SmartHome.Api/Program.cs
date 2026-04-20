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
using SmartHome.Domain.Simulation;
using SmartHome.Infrastructure.Simulation;
using SmartHome.Infrastructure.Simulation.Clock;
using SmartHome.Api.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Scalar.AspNetCore;
using SmartHome.Api.Validation;

// Create the application builder and load configuration/services
var builder = WebApplication.CreateBuilder(args);

// Register OpenAPI support
builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddProblemDetails();

// Register MVC controllers with enum-as-string JSON serialization
// Enables API clients to send/receive "Fast" instead of 5 for SimulationSpeed,
// producing self-documenting requests and cleaner Swagger docs.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));

// FluentValidation — register all AbstractValidator<T> implementations in this assembly.
// Combined with AddFluentValidationAutoValidation(), ASP.NET will invoke matching
// validators automatically before a request body reaches the controller action.
builder.Services.AddValidatorsFromAssemblyContaining<SetSimulationSpeedRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

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
builder.Services.AddScoped<DeviceRepository>();
builder.Services.AddScoped<IDeviceRepository>(sp => sp.GetRequiredService<DeviceRepository>());
builder.Services.AddScoped<ISimulationRepository>(sp => sp.GetRequiredService<DeviceRepository>());
builder.Services.AddScoped<SmartHomeDbSeeder>();
builder.Services.AddScoped<IDeviceFactory, DeviceFactory>();

// Register all device builders used by the factory
builder.Services.AddScoped<IDeviceBuilder, LightBuilder>();
builder.Services.AddScoped<IDeviceBuilder, FanBuilder>();
builder.Services.AddScoped<IDeviceBuilder, DoorLockBuilder>();
builder.Services.AddScoped<IDeviceBuilder, ThermostatBuilder>();

// Simulation speed registry — the source of truth for permitted multipliers.
// Singleton because allowed speeds are immutable for the app's lifetime.
builder.Services.AddSingleton<ISimulationSpeedRegistry, DefaultSimulationSpeedRegistry>();

// Simulation clock owns mutable simulation state (current time, active speed).
// Singleton so state persists across request scopes and background ticks.
builder.Services.AddSingleton<ISimulationClock, SimulationClock>();

// Register simulation service
builder.Services.AddScoped<ISimulationService, SimulationService>();

// Runs the background loop that updates thermostat behavior over time
builder.Services.AddHostedService<SimulationBackgroundService>();

// Build the application pipeline
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Expose OpenAPI document in development
    app.MapOpenApi();
    
    // Scalar UI — interactive API docs at /scalar/v1
    // Development-only; don't expose API surface in production without auth.
    app.MapScalarApiReference();
    // Redirect root to the Scalar docs in development for a friendly landing page.
    if (app.Environment.IsDevelopment())
        app.MapGet("/", () => Results.Redirect("/scalar/v1"));
}

// Exception handler MUST be registered before other middleware so it catches
// exceptions thrown anywhere downstream in the pipeline.
app.UseExceptionHandler();

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