using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Simulation;
using SmartHome.Infrastructure.Simulation.Clock;

namespace SmartHome.Infrastructure.Simulation;

/// <summary>
/// Coordinates simulation behavior: advancing ticks, applying ambient
/// temperature changes, and resetting devices. Delegates time/speed state
/// to <see cref="ISimulationClock"/> and persistence to <see cref="IDeviceRepository"/>.
/// </summary>
/// <param name="clock">The global simulation clock owning time and speed state.</param>
/// <param name="devices">The repository for general device domain operations.</param>
/// <param name="simulation">The repository for bulk simulation operations (ticking, resetting).</param>
public sealed class SimulationService(
    ISimulationClock clock,
    IDeviceRepository devices,
    ISimulationRepository simulation) : ISimulationService
{

    /// <inheritdoc />
    public SimulationSpeed Speed    => clock.Speed;
    
    /// <inheritdoc />
    public DateTime SimulationClock => clock.CurrentTime;

    /// <inheritdoc />
    public Task SetSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default)
    {
        clock.SetSpeed(speed);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task TickAsync(CancellationToken cancellationToken = default)
    {
        var tickables = await simulation.GetTickableAsync(cancellationToken);

        foreach (var device in tickables)
            device.Tick();

        await simulation.SaveChangesAsync(cancellationToken);
        clock.Advance(clock.BaseTickInterval);
    }

    /// <inheritdoc />
    public async Task SetAmbientTemperatureAsync(
        string location, int temperature, CancellationToken cancellationToken = default)
    {
        var thermostats = await devices.GetThermostatsByLocationAsync(location, cancellationToken);

        if (thermostats.Count == 0)
            throw new InvalidDomainOperationException(
                $"No thermostats exist at location '{location}'.");

        foreach (var thermostat in thermostats)
            thermostat.SetAmbientTemperature(temperature);

        await devices.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResetAllDevicesAsync(CancellationToken cancellationToken = default)
    {
        await simulation.ResetAllAsync(cancellationToken);
        await simulation.SaveChangesAsync(cancellationToken);
        clock.Reset();
    }
}