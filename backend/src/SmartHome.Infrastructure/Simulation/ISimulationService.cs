namespace SmartHome.Infrastructure.Simulation;

/// <summary>
/// Defines operations for managing and advancing the smart home simulation,
/// including time progression, environment updates, and global resets.
/// </summary>
public interface ISimulationService
{
    /// <summary>
    /// The current simulation speed multiplier (e.g., 1x, 2x, 5x, 10x).
    /// </summary>
    int SpeedMultiplier { get; }

    /// <summary>
    /// The current simulation clock time.
    /// </summary>
    DateTime SimulationClock { get; }

    /// <summary>
    /// Sets the simulation speed multiplier.
    /// Throws <see cref="ArgumentException"/> if the multiplier is invalid.
    /// </summary>
    Task SetSpeedAsync(int multiplier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Advances the simulation by one tick.
    /// Updates thermostat states and ambient temperatures based on current conditions.
    /// </summary>
    Task TickAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all devices in the system to their default states.
    /// </summary>
    Task ResetAllDevicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the ambient temperature for a specific location.
    /// Throws <see cref="ArgumentException"/> if the temperature is invalid.
    /// Throws <see cref="InvalidOperationException"/> if the location does not exist.
    /// </summary>
    Task SetAmbientTemperatureAsync(string location, int temperature, CancellationToken cancellationToken = default);
}