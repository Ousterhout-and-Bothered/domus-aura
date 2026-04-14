namespace SmartHome.Domain.Device;


/// <summary>
/// Defines the core contract for all smart home devices.
/// Every device regardless of type must expose an identity,
/// a name, a location, and the ability to report its on/off state.
/// </summary>
public interface IDevice
{
    
    /// <summary>
    /// The unique identifier for this device.
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// The human-readable name of the device.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// The room or area the device is located in.
    /// </summary>
    string Location { get; }
    
    /// <summary>
    /// The type of device (Light, Fan, Thermostat, DoorLock).
    /// </summary>
    DeviceType Type { get; }


    /// <summary>
    /// Returns true when the device is in an active "on" state.
    /// DoorLock always returns true.
    /// Powered devices return true only when powered on.
    /// </summary>
    bool IsOn();
}