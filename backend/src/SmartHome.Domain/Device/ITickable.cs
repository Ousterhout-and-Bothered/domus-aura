namespace SmartHome.Domain.Device;

/// <summary>
/// Defines behavior for devices whose internal state advances with simulation time.
/// </summary>
/// <remarks>
/// The simulation engine invokes <see cref="Tick"/> on each <see cref="ITickable"/>
/// device during every simulation cycle. This allows new time-driven device types
/// to be introduced without modifying simulation orchestration code.
/// </remarks>
public interface ITickable
{
    /// <summary>
    /// Advances the device's internal state by one simulation tick.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the device's observable state changed as a result of the tick;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool Tick();
}