namespace SmartHome.Domain.Device;

/// <summary>
/// Abstract base class for devices whose internal state advances over simulation time.
/// Provides a common foundation for time-driven behavior while allowing subclasses
/// to define their own tick logic.
/// </summary>
/// <remarks>
/// Implementing <see cref="ITickable"/> allows these devices to participate in the
/// simulation loop without the infrastructure needing to know their concrete type.
/// </remarks>
public abstract class TickableDevice : Device, ITickable
{
    protected TickableDevice() { }

    protected TickableDevice(Guid id, string name, string location, DeviceType type)
        : base(id, name, location, type) { }

    protected TickableDevice(string name, string location, DeviceType type)
        : base(name, location, type) { }

    /// <summary>
    /// Advances the device state by one simulation tick.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the device's observable state changed as a result of the tick;
    /// otherwise, <c>false</c>.
    /// </returns>
    public abstract bool Tick();
}
