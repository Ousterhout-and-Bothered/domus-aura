namespace SmartHome.Domain.Device.Fan;


/// <summary>
/// Represents a smart fan device that supports power and speed control.
/// Speed is retained when the fan is powered off and restored when powered back on.
/// </summary>
public sealed class Fan : PoweredDevice
{
    
    
    /// <summary>
    /// The current speed of the fan.
    /// </summary>
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
    
    
    /// <summary>
    /// Sets the speed of the fan.
    /// Throws <see cref="InvalidOperationException"/> if the fan is off.
    /// </summary>
    public void SetSpeed(FanSpeed speed)
    {
        if (PowerState != PowerState.On)
            throw new InvalidOperationException("Speed can only be changed while the fan is on.");

        Speed = speed;
    }
}