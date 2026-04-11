using SmartHome.Domain.Enum;

namespace Smarthome.Domain.Device;

public sealed class Fan : PoweredDevice
{
    
    public FanSpeed Speed { get; private set; }

    // Required for EF Core
    private Fan()
    {
        Type = DeviceType.Fan;
        Speed = FanSpeed.Medium;
    }

    public Fan(string name, string location) : base(name, location, DeviceType.Fan)
    {
        Speed = FanSpeed.Medium;
    }
    
    public void SetSpeed(FanSpeed speed)
    {
        if (PowerState != PowerState.On)
            throw new InvalidOperationException("Speed can only be changed while the fan is on.");

        Speed = speed;
    }
}