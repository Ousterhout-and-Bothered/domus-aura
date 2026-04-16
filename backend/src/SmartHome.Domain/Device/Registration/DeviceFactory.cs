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
    /// Thrown if <paramref name="name"/> or <paramref name="location"/> is invalid.
    /// Validation is performed by the device constructor.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="type"/> is not a supported device type.
    /// </exception>
    public Device Create(string name, string location, DeviceType type)
    {
        if (!_builders.TryGetValue(type, out var builder))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type), type, $"Unsupported device type: {type}.");
        }
        return builder.Build(name, location);
    }

}