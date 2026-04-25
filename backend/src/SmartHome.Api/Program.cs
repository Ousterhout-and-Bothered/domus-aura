using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Device;
using SmartHome.Infrastructure.Device.Repository;
using SmartHome.Infrastructure.Persistence;
using SmartHome.Infrastructure.Persistence.Seed;
using SmartHome.Domain.Device.Registration;
using SmartHome.Domain.Device.Commands;
using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Device.Fan;
using SmartHome.Domain.Device.DoorLock;
using SmartHome.Domain.Device.Thermostat;
using SmartHome.Domain.Simulation;
using SmartHome.Infrastructure.Simulation;
using SmartHome.Infrastructure.Simulation.Clock;
using SmartHome.Infrastructure.Device.Service;
using SmartHome.Api.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Scalar.AspNetCore;
using SmartHome.Api.Validation;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using SmartHome.Infrastructure.Device.Events;

/*
 * Domus Aura - Smart Home Simulation API
 * 
 * This file serves as the entry point for the Web API. It is responsible for:
 * 1. Configuring the Dependency Injection (DI) container.
 * 2. Setting up the request processing pipeline (middleware).
 * 3. Initializing infrastructure such as SQLite and automatic database seeding.
 */

// Create the application builder and load configuration/services
var builder = WebApplication.CreateBuilder(args);

// Register OpenAPI support
builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.TypeInfoResolver = ConfigureDevicePolymorphism();
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else if (builder.Environment.IsDevelopment())
        {
            // Fallback to localhost during development if no origins are configured
            policy.WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        // If not in Development and no origins are configured, the policy will remain closed
        // to prevent accidental exposure.
    });
});

builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddProblemDetails();

// Register MVC controllers with enum-as-string JSON serialization and
// polymorphic device serialization. Polymorphism config is layered separately
// from the domain entity (no [JsonDerivedType] attributes on Device) so the
// domain remains framework-agnostic.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.TypeInfoResolver = ConfigureDevicePolymorphism();
    });

// FluentValidation — register all AbstractValidator<T> implementations in this assembly.
// Combined with AddFluentValidationAutoValidation(), ASP.NET will invoke matching
// validators automatically before a request body reaches the controller action.
builder.Services.AddValidatorsFromAssemblyContaining<SetSimulationSpeedRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// --- SQLite Connection String and Directory Resolution ---
// This ensures that the SQLite database directory exists and the connection string 
// uses an absolute path, fixing "SQLite Error 14: unable to open database file".
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(connectionString))
{
    // Resolve "Data Source" or "DataSource" path
    var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
    var dataSourcePart = parts.FirstOrDefault(p =>
        p.TrimStart().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) ||
        p.TrimStart().StartsWith("DataSource=", StringComparison.OrdinalIgnoreCase));

    if (dataSourcePart != null)
    {
        var dbPath = dataSourcePart.Split('=', 2)[1].Trim();

        // If the path is relative, resolve it against the application root
        if (!string.IsNullOrEmpty(dbPath) && !Path.IsPathRooted(dbPath))
        {
            var absoluteDbPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, dbPath));

            // Ensure the directory exists
            var directory = Path.GetDirectoryName(absoluteDbPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Rebuild the connection string with the absolute path
            var otherParts = parts
                .Where(p => !p.TrimStart().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) &&
                            !p.TrimStart().StartsWith("DataSource=", StringComparison.OrdinalIgnoreCase))
                .ToList();

            connectionString = $"Data Source={absoluteDbPath};{string.Join(';', otherParts)}";
        }
    }
}
else
{
    // Fallback for development if not in appsettings
    var dataDirectory = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "data"));
    Directory.CreateDirectory(dataDirectory);
    var databasePath = Path.Combine(dataDirectory, "smarthome.db");
    connectionString = $"Data Source={databasePath};Cache=Shared;Default Timeout=30;";
}

// Register EF Core DbContext using SQLite persistence
builder.Services.AddDbContext<SmartHomeDbContext>(options =>
    options.UseSqlite(connectionString));

// Register persistence and seeding services
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<ISimulationRepository, SimulationRepository>();
builder.Services.AddScoped<SmartHomeDbSeeder>();
builder.Services.AddScoped<IDeviceFactory, DeviceFactory>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IDeviceCommandFactory, DeviceCommandFactory>();
builder.Services.AddSingleton<IThermostatStrategyProvider, ThermostatStrategyProvider>();
builder.Services.AddSingleton<DeviceEventBroker>();
builder.Services.AddSingleton<IDeviceEventPublisher>(sp =>
    sp.GetRequiredService<DeviceEventBroker>());
builder.Services.AddSingleton<IDeviceEventStream>(sp =>
    sp.GetRequiredService<DeviceEventBroker>());

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
    // This endpoint is excluded from OpenAPI to prevent it from being treated as part of the API contract.
    app.MapGet("/", () => Results.Redirect("/scalar/v1"))
        .ExcludeFromDescription();
}

// Exception handler MUST be registered before other middleware so it catches
// exceptions thrown anywhere downstream in the pipeline.
app.UseExceptionHandler();

// Enable CORS before mapping controllers
app.UseCors();

// Redirect HTTP requests to HTTPS
app.UseHttpsRedirection();

// Log resolved connection string for debugging startup issues (masking potential sensitive info if needed, but SQLite is local)
app.Logger.LogDebug("Using Connection String: {ConnectionString}", connectionString);

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

static DefaultJsonTypeInfoResolver ConfigureDevicePolymorphism() =>
    new DefaultJsonTypeInfoResolver
    {
        Modifiers =
        {
            typeInfo =>
            {
                if (typeInfo.Type == typeof(Device))
                {
                    typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                    {
                        TypeDiscriminatorPropertyName = "$type",
                        DerivedTypes =
                        {
                            new JsonDerivedType(typeof(Light), "Light"),
                            new JsonDerivedType(typeof(Fan), "Fan"),
                            new JsonDerivedType(typeof(Thermostat), "Thermostat"),
                            new JsonDerivedType(typeof(DoorLock), "DoorLock")
                        }
                    };
                }
            }
        }
    };

// Map attribute-routed API controllers
app.MapControllers();

// Start the web application
app.Run();