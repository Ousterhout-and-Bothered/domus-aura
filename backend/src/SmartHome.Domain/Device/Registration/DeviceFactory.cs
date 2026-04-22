using SmartHome.Domain.Common;

namespace SmartHome.Domain.Device.Registration;

/// <summary>
/// Creates devices by delegating to type-specific <see cref="IDeviceBuilder"/> instances.
/// Builders are injected via DI; this factory never needs modification when new device types are added.
/// </summary>
public sealed class DeviceFactory(IEnumerable<IDeviceBuilder> builders) : IDeviceFactory
{
    private readonly Dictionary<DeviceType, IDeviceBuilder> _builders =
        builders.ToDictionary(b => b.HandledType);


    /// <summary>
    /// Creates a new device of the specified type.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="name"/> or <paramref name="location"/> is invalid,
    /// or if <paramref name="type"/> is not a supported device type.
    /// Validation is performed by the device constructor or <see cref="Guard.EnumDefined"/>.
    /// </exception>
    public Device Create(string name, string location, DeviceType type)
    {
        Guard.EnumDefined(type, nameof(type));
        var builder = _builders[type];
        return builder.Build(name, location);
    }

}