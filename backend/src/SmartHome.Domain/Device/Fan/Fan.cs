using SmartHome.Domain.Common;
using SmartHome.Domain.Common.Exceptions;

namespace SmartHome.Domain.Device.Fan;

/// <summary>
/// Represents a smart fan device that supports power and speed control.
/// Speed is retained when the fan is powered off and restored when powered back on.
/// </summary>
public sealed class Fan : PoweredDevice, IFanControllable
{
    /// <summary>
    /// Gets the current speed of the fan.
    /// </summary>
    public FanSpeed Speed { get; private set; }

    // Required for EF Core
    private Fan()
    {
        Type = DeviceType.Fan;
        Speed = FanSpeed.Medium;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Fan"/> class with a specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier for the fan.</param>
    /// <param name="name">The user-facing name of the fan.</param>
    /// <param name="location">The location of the fan.</param>
    public Fan(Guid id, string name, string location)
        : base(id, name, location, DeviceType.Fan)
    {
        Speed = FanSpeed.Medium;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Fan"/> class with a generated identifier.
    /// </summary>
    /// <param name="name">The user-facing name of the fan.</param>
    /// <param name="location">The location of the fan.</param>
    public Fan(string name, string location) : base(name, location, DeviceType.Fan)
    {
        Speed = FanSpeed.Medium;
    }

    /// <summary>
    /// Sets the speed of the fan.
    /// Throws <see cref="InvalidDomainOperationException"/> if the fan is off.
    /// Throws <see cref="InvalidDomainArgumentException"/> if the speed is not defined.
    /// </summary>
    /// <param name="speed">The desired speed to set.</param>
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
    /// <returns>A string representation of the fan including its current state.</returns>
    public override string ToString() =>
        $"{GetType().Name}(Id={Id}, Name='{Name}', Location='{Location}', " +
        $"Power={PowerState}, Speed={Speed})";
}