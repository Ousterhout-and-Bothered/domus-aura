namespace SmartHome.Domain.Device.Registration;

/// <summary>
/// Creates new smart home devices from a registration request.
/// To add a new device type, add a case here and implement the device class —
/// nothing else in the codebase needs to change.
/// </summary>
public sealed class DeviceFactory : IDeviceFactory
{
    
    /// <summary>
    /// Creates a new device of the specified type, initialized in its default state.
    /// Throws <see cref="ArgumentOutOfRangeException"/> if the type is not supported.
    /// </summary>
    public Device Create(string name, string location, DeviceType type) => type switch
    {
        DeviceType.Light => new Light.Light(name, location),
        DeviceType.Fan => new Fan.Fan(name, location),
        DeviceType.Thermostat => new Thermostat.Thermostat(name, location),
        DeviceType.DoorLock => new DoorLock.DoorLock(name, location),
        _ => throw new ArgumentOutOfRangeException(nameof(type),
            $"Unsupported device type: {type}.")
    };
}