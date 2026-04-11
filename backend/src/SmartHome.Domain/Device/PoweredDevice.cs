using SmartHome.Domain.Enum;

namespace SmartHome.Domain.Device;

public abstract class PoweredDevice : Device
{
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
    
    public virtual void TurnOff()
    {
        PowerState = PowerState.Off;
    }

    public override bool IsOn() => PowerState == PowerState.On;
}