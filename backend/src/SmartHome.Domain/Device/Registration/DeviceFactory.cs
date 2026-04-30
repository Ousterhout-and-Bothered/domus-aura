using SmartHome.Domain.Common.Exceptions;
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
    /// Creates a new device of the specified type by delegating to the appropriate registered builder.
    /// </summary>
    /// <param name="name">The name to assign to the new device.</param>
    /// <param name="location">The location where the device is installed.</param>
    /// <param name="type">The type of device to create.</param>
    /// <returns>A newly initialized <see cref="Device"/> instance.</returns>
    /// <exception cref="InvalidDomainArgumentException">
    /// Thrown if <paramref name="name"/> or <paramref name="location"/> is invalid, 
    /// or if <paramref name="type"/> is not a defined value in the <see cref="DeviceType"/> enum.
    /// </exception>
    /// <exception cref="InvalidDomainOperationException">
    /// Thrown if the <paramref name="type"/> is valid but no corresponding <see cref="IDeviceBuilder"/> 
    /// has been registered in the system, which typically indicates a DI misconfiguration.
    /// </exception>
    public Device Create(string name, string location, DeviceType type)
    {
        Guard.EnumDefined(type, nameof(type));

        if (!_builders.TryGetValue(type, out var builder))
        {
            throw new InvalidDomainOperationException(
                $"No builder registered for device type: {type}. Check DI configuration.");
        }

        return builder.Build(name, location);
    }

}