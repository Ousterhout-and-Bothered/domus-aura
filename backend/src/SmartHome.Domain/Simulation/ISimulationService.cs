namespace SmartHome.Domain.Simulation;

/// <summary>
/// Defines operations for managing and advancing the smart home simulation,
/// including time progression, environment updates, and global resets.
/// </summary>
public interface ISimulationService
{
    /// <summary>
    /// The current simulation speed.
    /// </summary>
    SimulationSpeed Speed { get; }

    /// <summary>
    /// The current simulation clock time.
    /// </summary>
    DateTime SimulationClock { get; }

    /// <summary>
    /// Sets the simulation speed.
    /// Throws <see cref="ArgumentOutOfRangeException"/> if the speed is not permitted by the registry.
    /// </summary>
    Task SetSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default);

    /// <summary>
    /// Advances the simulation by one tick — ticks all ITickable devices and advances the clock.
    /// </summary>
    Task TickAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all devices in the system to their default states and resets the clock.
    /// </summary>
    Task ResetAllDevicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the ambient temperature for a specific location.
    /// Throws <see cref="InvalidOperationException"/> if no thermostats exist at the location.
    /// </summary>
    Task SetAmbientTemperatureAsync(
        string location, int temperature, CancellationToken cancellationToken = default);
}