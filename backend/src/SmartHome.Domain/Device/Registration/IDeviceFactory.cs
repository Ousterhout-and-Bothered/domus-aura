namespace SmartHome.Domain.Device.Registration;

/// <summary>
/// Creates new smart home devices from a registration request.
/// Each device is initialized in its correct default state with a generated ID.
/// Adding a new device type requires only a new case here — nothing else changes.
/// </summary>
public interface IDeviceFactory
{
    
    /// <summary>
    /// Creates a new device of the specified type.
    /// Throws <see cref="ArgumentException"/> if name or location is null or whitespace.
    /// Throws <see cref="ArgumentOutOfRangeException"/> if type is not supported.
    /// </summary>
    Device Create(string name, string location, DeviceType type);
}