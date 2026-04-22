namespace SmartHome.Domain.Device;

/// <summary>
/// Abstract base class for all devices that require a simulation tick
/// to update their state over time (e.g., Temperature changing).
/// Enables OCP by allowing the infrastructure to discover all tickable 
/// devices through a common base type rather than hardcoding.
/// </summary>
public abstract class TickableDevice : Device, ITickable
{
    protected TickableDevice() : base() { }

    protected TickableDevice(string name, string location, DeviceType type)
        : base(name, location, type) { }

    /// <summary>
    /// Advances the simulation by one tick.
    /// Subclasses provide the concrete behavior.
    /// </summary>
    public abstract void Tick();
}
