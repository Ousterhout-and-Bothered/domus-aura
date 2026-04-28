using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Scalar.AspNetCore;
using SmartHome.Infrastructure.Device.Repository;
using SmartHome.Infrastructure.Device.Service;
using SmartHome.Infrastructure.Device.Events;
using SmartHome.Infrastructure.Persistence;
using SmartHome.Infrastructure.Simulation;
using SmartHome.Infrastructure.Simulation.Clock;
using SmartHome.Infrastructure.Persistence.Seed;
using SmartHome.Infrastructure.Scene;
using SmartHome.Domain.Scene;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Registration;
using SmartHome.Domain.Device.Commands;
using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Device.Fan;
using SmartHome.Domain.Device.DoorLock;
using SmartHome.Domain.Device.Thermostat;
using SmartHome.Domain.Simulation;
using SmartHome.Api.Middleware;
using SmartHome.Api.Validation;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;


var builder = WebApplication.CreateBuilder(args);

// OpenAPI for local exploration (Scalar UI enabled in dev only)
builder.Services.AddOpenApi();

// JSON config — keep enums readable and handle device polymorphism
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.TypeInfoResolver = ConfigureDevicePolymorphism();
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.TypeInfoResolver = ConfigureDevicePolymorphism();
    });

// CORS — allow frontend during dev, otherwise rely on config
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        if (origins.Length > 0)
        {
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// Centralized error handling (RFC 9457 ProblemDetails)
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddProblemDetails();

// Request validation at the API boundary
builder.Services.AddValidatorsFromAssemblyContaining<SetSimulationSpeedRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// JWT auth (Keycloak)
// Authority/Audience come from config (docker-compose or appsettings)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.RequireHttpsMetadata =
            builder.Configuration.GetValue<bool?>("Authentication:RequireHttpsMetadata")
            ?? !builder.Environment.IsDevelopment();
        options.TokenValidationParameters.ValidIssuer =
            builder.Configuration["Authentication:ValidIssuer"];
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// SQLite setup — resolves relative paths and ensures directory exists
var connectionString = ResolveSqliteConnectionString(builder);

builder.Services.AddDbContext<SmartHomeDbContext>(options =>
    options.UseSqlite(connectionString));

// Repositories and persistence
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<ISimulationRepository, SimulationRepository>();
builder.Services.AddScoped<SmartHomeDbSeeder>();
builder.Services.AddScoped<ISceneRepository, SceneRepository>();

// Domain services
builder.Services.AddScoped<IDeviceFactory, DeviceFactory>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IDeviceCommandFactory, DeviceCommandFactory>();
builder.Services.AddScoped<ISceneResolver, SceneResolver>();
builder.Services.AddScoped<ISceneService, SceneService>();

// Device builders (factory registration)
builder.Services.AddScoped<IDeviceBuilder, LightBuilder>();
builder.Services.AddScoped<IDeviceBuilder, FanBuilder>();
builder.Services.AddScoped<IDeviceBuilder, DoorLockBuilder>();
builder.Services.AddScoped<IDeviceBuilder, ThermostatBuilder>();

// Strategy provider (thermostat behavior)
builder.Services.AddSingleton<IThermostatStrategyProvider, ThermostatStrategyProvider>();

// Simulation
builder.Services.AddSingleton<ISimulationSpeedRegistry, DefaultSimulationSpeedRegistry>();
builder.Services.AddSingleton<ISimulationClock, SimulationClock>();
builder.Services.AddScoped<ISimulationService, SimulationService>();
builder.Services.AddHostedService<SimulationBackgroundService>();

// Event system (SSE support)
builder.Services.AddSingleton<DeviceEventBroker>();
builder.Services.AddSingleton<IDeviceEventPublisher>(sp =>
    sp.GetRequiredService<DeviceEventBroker>());
builder.Services.AddSingleton<IDeviceEventStream>(sp =>
    sp.GetRequiredService<DeviceEventBroker>());

var app = builder.Build();

// Dev-only API UI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    app.MapGet("/", () => Results.Redirect("/scalar/v1"))
        .ExcludeFromDescription();
}

// Middleware Pipeline
app.UseExceptionHandler();
app.UseCors();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Apply migrations and seed on startup
app.Logger.LogInformation("Using SQLite connection string: {ConnectionString}", connectionString);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<SmartHomeDbSeeder>();
    await seeder.SeedAsync();
}

app.MapControllers();

await app.RunAsync();


// Helpers

static string ResolveSqliteConnectionString(WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        var dataDir = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "data"));
        Directory.CreateDirectory(dataDir);

        return $"Data Source={Path.Combine(dataDir, "smarthome.db")};";
    }

    var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);

    var dataSource = parts.FirstOrDefault(p =>
        p.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase));

    if (dataSource == null) return connectionString;

    var path = dataSource.Split('=')[1].Trim();

    if (Path.IsPathRooted(path)) return connectionString;

    var absolute = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, path));
    Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);

    var others = parts.Where(p => !p.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase));

    return $"Data Source={absolute};{string.Join(';', others)}";
}

static DefaultJsonTypeInfoResolver ConfigureDevicePolymorphism() =>
    new()
    {
        Modifiers =
        {
            typeInfo =>
            {
                if (typeInfo.Type != typeof(Device)) return;

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
    };