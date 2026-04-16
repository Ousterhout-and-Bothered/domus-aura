using SmartHome.Domain.Device.Registration;

namespace SmartHome.Domain.Device.Fan;

/// <summary>
/// Constructs <see cref="Fan"/> instances for the <see cref="DeviceFactory"/>.
/// </summary>
public sealed class FanBuilder : IDeviceBuilder
{
    public DeviceType HandledType => DeviceType.Fan;

    public Device Build(string name, string location) => new Fan(name, location);
}