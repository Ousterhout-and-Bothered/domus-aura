namespace SmartHome.Domain.Device;


/// <summary>
/// Marker for devices whose internal state advances with simulation time.
/// The simulation harness will call <see cref="Tick"/> on every ITickable
/// device on each tick, so new tickable device types can be added without
/// modifying simulation code (Open/Closed Principle).
/// </summary>
public interface ITickable
{
    void Tick();
}