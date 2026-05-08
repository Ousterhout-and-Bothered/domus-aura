namespace SmartHome.Domain.Device;

/// <summary>
/// Represents a device that supports explicit power control.
/// Powered devices can be turned on and off, while retaining
/// their functional substate (brightness, speed, mode, etc.)
/// between power cycles.
/// </summary>
public interface IPowerable
{
    /// <summary>
    /// Gets the current externally visible power state of the device.
    /// This value may differ from the device's internal operational state
    /// for reporting or filtering purposes.
    /// </summary>
    PowerState PowerState { get; }

    /// <summary>
    /// Gets a value indicating whether the device is currently powered on
    /// at the domain level.
    /// Unlike <see cref="PowerState"/>, this property reflects the true
    /// operational power status used for command execution and state transitions.
    /// </summary>
    bool IsPoweredOn { get; }

    /// <summary>
    /// Powers the device on and restores its last known operational substate.
    /// </summary>
    void TurnOn();

    /// <summary>
    /// Powers the device off while retaining its current substate
    /// for restoration on the next power-on operation.
    /// </summary>
    void TurnOff();
}