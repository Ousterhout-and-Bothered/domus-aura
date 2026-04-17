using SmartHome.Domain.Device.Registration;

namespace SmartHome.Domain.Device.Light;

/// <summary>
/// Constructs <see cref="Light"/> instances for the <see cref="DeviceFactory"/>.
/// </summary>
public sealed class LightBuilder : IDeviceBuilder
{
    public DeviceType HandledType => DeviceType.Light;

    public Device Build(string name, string location) => new Light(name, location);
}