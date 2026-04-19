using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.DoorLock;
using SmartHome.Domain.Device.Fan;
using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Device.Thermostat;
using SmartHome.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace SmartHome.Infrastructure.Simulation;

/// <summary>
/// Coordinates simulation behavior, including time progression,
/// thermostat updates, ambient temperature changes, and system resets.
/// </summary>
public sealed class SimulationService : ISimulationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _lock = new();

    private int _speedMultiplier = 1;
    private DateTime _simulationClock = DateTime.UtcNow;

    public int SpeedMultiplier
    {
        get
        {
            lock (_lock)
            {
                // Thread-safe read of simulation speed
                return _speedMultiplier;
            }
        }
    }

    public DateTime SimulationClock
    {
        get
        {
            lock (_lock)
            {
                // Thread-safe read of simulation clock
                return _simulationClock;
            }
        }
    }

    public SimulationService(IServiceScopeFactory scopeFactory)
    {
        // Inject scope factory
        _scopeFactory = scopeFactory;
    }

    public Task SetSpeedAsync(int multiplier, CancellationToken cancellationToken = default)
    {
        // Validate allowed speed values per specification
        if (multiplier is not (1 or 2 or 5 or 10))
            throw new ArgumentOutOfRangeException(nameof(multiplier), "Speed must be 1, 2, 5, or 10.");

        lock (_lock)
        {
            // Update simulation speed (thread-safe)
            _speedMultiplier = multiplier;
        }

        return Task.CompletedTask;
    }

    public async Task TickAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

        // Retrieve all thermostats
        var thermostats = await dbContext.Devices
            .OfType<Thermostat>()
            .ToListAsync(cancellationToken);

        foreach (var thermostat in thermostats)
        {
            // Advance thermostat state machine
            thermostat.Tick();
        }

        lock (_lock)
        {
            // Advance simulation clock based on speed multiplier
            _simulationClock = _simulationClock.AddSeconds(5.0 / _speedMultiplier);
        }

        // Persist updated device states
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetAmbientTemperatureAsync(string location, int temperature, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

        // Find all thermostats in the specified location
        var thermostats = await dbContext.Devices
            .OfType<Thermostat>()
            .Where(t => t.Location == location)
            .ToListAsync(cancellationToken);

        foreach (var thermostat in thermostats)
        {
            // Update ambient temperature
            thermostat.SetAmbientTemperature(temperature);
        }

        // Persist ambient temperature changes
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetAllDevicesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

        // Load all devices for reset operation
        var devices = await dbContext.Devices.ToListAsync(cancellationToken);

        foreach (var device in devices)
            {
            device.ResetToDefaults();
            await _deviceRepository.UpdateAsync(device);
            }
        }

        lock (_lock)
        {
            // Reset simulation clock to current time
            _simulationClock = DateTime.UtcNow;
        }

        // Persist all reset changes
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
