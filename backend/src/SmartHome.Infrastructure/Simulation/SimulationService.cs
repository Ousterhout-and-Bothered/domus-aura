using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Simulation;
using SmartHome.Infrastructure.Simulation.Clock;

namespace SmartHome.Infrastructure.Simulation;

/// <summary>
/// Coordinates simulation behavior: advancing ticks, applying ambient
/// temperature changes, and resetting devices. Delegates time/speed state
/// to <see cref="ISimulationClock"/> and persistence to <see cref="IDeviceRepository"/>.
/// </summary>
public sealed class SimulationService(
    ISimulationClock clock,
    IDeviceRepository devices,
    ISimulationRepository simulation) : ISimulationService
{
    // One tick advances simulated time by this amount. Real-time pacing is
    // controlled separately by the background service using Speed.
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);

    public SimulationSpeed Speed    => clock.Speed;
    public DateTime SimulationClock => clock.CurrentTime;

    public Task SetSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default)
    {
        clock.SetSpeed(speed);
        return Task.CompletedTask;
    }

    public async Task TickAsync(CancellationToken cancellationToken = default)
    {
        var tickables = await simulation.GetTickableAsync(cancellationToken);

        foreach (var device in tickables)
            device.Tick();

        clock.Advance(TickInterval);
        await devices.SaveChangesAsync(cancellationToken);
    }

    public async Task SetAmbientTemperatureAsync(
        string location, int temperature, CancellationToken cancellationToken = default)
    {
        var thermostats = await devices.GetThermostatsByLocationAsync(location, cancellationToken);

        if (thermostats.Count == 0)
            throw new InvalidOperationException(
                $"No thermostats exist at location '{location}'.");

        foreach (var thermostat in thermostats)
            thermostat.SetAmbientTemperature(temperature);

        await devices.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetAllDevicesAsync(CancellationToken cancellationToken = default)
    {
        await simulation.ResetAllAsync(cancellationToken);
        clock.Reset();
    }
}