namespace SmartHome.Domain.Device.Registration;

using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Device.Fan;
using SmartHome.Domain.Device.Thermostat;
using SmartHome.Domain.Device.DoorLock;

/// <summary>
/// Creates new smart home devices from a registration request.
/// To add a new device type, add a case here and implement the device class —
/// nothing else in the codebase needs to change.
/// </summary>
public sealed class DeviceFactory : IDeviceFactory
{

    /// Name and location are validated by the device constructors.
    /// Throws <see cref="ArgumentException"/> if name or location is invalid.
    /// Throws <see cref="ArgumentOutOfRangeException"/> if the type is not supported.
    public Device Create(string name, string location, DeviceType type) => type switch
        {
            DeviceType.Light => new Light(name, location),
            DeviceType.Fan => new Fan(name, location),
            DeviceType.Thermostat => new Thermostat(name, location),
            DeviceType.DoorLock => new DoorLock(name, location),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type,
                $"Unsupported device type: {type}.")
        };
    
}