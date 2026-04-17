namespace SmartHome.Domain.Device.Registration;

/// <summary>
/// Creates new smart home devices from a registration request.
/// Each device is initialized by its builder with a generated ID and default state.
/// </summary>
/// <remarks>
/// New device types are added by implementing <see cref="IDeviceBuilder"/> and
/// registering the implementation in DI. No changes to this interface, <see cref="DeviceFactory"/>,
/// or existing builders are required.
/// </remarks>
public interface IDeviceFactory
{
    
    /// <summary>
    /// Creates a new device of the specified type.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="name"/> or <paramref name="location"/> is null or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if no <see cref="IDeviceBuilder"/> is registered for <paramref name="type"/>.
    /// </exception>
    Device Create(string name, string location, DeviceType type);
}