namespace SmartHome.Domain.Device.Fan;

/// <summary>
/// Defines the contract for devices that support variable fan speed control.
/// Speed may only be changed while the device is powered on.
/// </summary>
public interface IFanControllable
{
    /// <summary>
    /// The current speed of the fan (Low, Medium, or High).
    /// </summary>
    FanSpeed Speed { get; }
    
    /// <summary>
    /// Sets the speed of the fan.
    /// Throws <see cref="InvalidOperationException"/> if the device is off.
    /// </summary>
    void SetSpeed(FanSpeed speed);
}