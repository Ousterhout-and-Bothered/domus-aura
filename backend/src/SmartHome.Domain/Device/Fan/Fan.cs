using SmartHome.Domain.Common;

namespace SmartHome.Domain.Device.Fan;

/// <summary>
/// Represents a smart fan device that supports power and speed control.
/// Speed is retained when the fan is powered off and restored when powered back on.
/// </summary>
/// <remarks>
/// As of the implicit-power-on refactor, calling <see cref="SetSpeed"/> on a
/// powered-off fan no longer throws. The command layer (<c>SetSpeedCommand</c>)
/// is responsible for invoking <see cref="PoweredDevice.TurnOn"/> first when
/// needed.
/// </remarks>
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
    /// Throws <see cref="SmartHome.Domain.Common.Exceptions.InvalidDomainArgumentException"/>
    /// if the speed value is not defined on the enum.
    /// </summary>
    /// <param name="speed">The desired speed to set.</param>
    public void SetSpeed(FanSpeed speed)
    {
        // No power-state guard — command layer ensures precondition.
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