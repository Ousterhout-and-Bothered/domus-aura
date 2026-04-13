using SmartHome.Domain.Enum;
using SmartHome.Domain.Device.Interface;

namespace SmartHome.Domain.Device;


/// <summary>
/// Base class for all devices that have an explicit power state.
/// Substate (brightness, speed, etc.) is only meaningful when the device is on.
/// </summary>
public abstract class PoweredDevice : Device, IPowerable
{
    
    /// <summary>
    /// The current power state of the device. Defaults to Off.
    /// </summary>
    public PowerState PowerState { get; protected set; }

    // Required for EF Core
    protected PoweredDevice()
    {
        PowerState = PowerState.Off;
    }

    protected PoweredDevice(string name, string location, DeviceType type)
        : base(name, location, type)
    {
        PowerState = PowerState.Off;
    }

    public virtual void TurnOn()
    {
        PowerState = PowerState.On;
    }
    
    public virtual void TurnOff()
    {
        PowerState = PowerState.Off;
    }

    public override bool IsOn() => PowerState == PowerState.On;
}