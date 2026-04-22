using SmartHome.Domain.Common;

namespace SmartHome.Domain.Device.Fan;


/// <summary>
/// Represents a smart fan device that supports power and speed control.
/// Speed is retained when the fan is powered off and restored when powered back on.
/// </summary>
public sealed class Fan : PoweredDevice, IFanControllable
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
    /// Throws <see cref="ArgumentOutOfRangeException"/> is the speed is not defined.
    /// </summary>
    public void SetSpeed(FanSpeed speed)
    {
        Guard.AgainstInvalidState(PowerState == PowerState.On, "Speed can only be changed while the fan is on.");

        Guard.EnumDefined(speed, nameof(speed));

        Speed = speed;
    }

    /// <summary>
    /// Resets powered device attributes for the fan to their default values.
    /// </summary>
    protected override void ResetPoweredDefaults()
    {
        // Fans default to Medium when powered on
        Speed = FanSpeed.Medium;
    }

    /// <summary>
    /// Log-friendly representation including power and speed.
    /// </summary>
    public override string ToString() =>
        $"{GetType().Name}(Id={Id}, Name='{Name}', Location='{Location}', " +
        $"Power={PowerState}, Speed={Speed})";
}