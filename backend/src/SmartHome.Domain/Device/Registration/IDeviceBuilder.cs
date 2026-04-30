namespace SmartHome.Domain.Device.Registration;

/// <summary>
/// Builds a single type of <see cref="Device"/>.
/// Each device type has its own builder, registered via DI and dispatched by <see cref="DeviceFactory"/>.
/// </summary>
/// <remarks>
/// Adding a new device type means adding a new <see cref="IDeviceBuilder"/> implementation
/// and registering it in DI. No changes to <see cref="DeviceFactory"/> or existing builders.
/// </remarks>
public interface IDeviceBuilder
{
    /// <summary>The device type this builder handles.</summary>
    DeviceType HandledType { get; }

    /// <summary>
    /// Constructs a new device of the handled type.
    /// Implementations must return a device with a freshly generated ID,
    /// placed in its correct initial state.
    /// </summary>
    Device Build(string name, string location);
}