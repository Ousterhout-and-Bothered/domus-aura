using SmartHome.Domain.Device.Registration;

namespace SmartHome.Domain.Device.DoorLock;

/// <summary>
/// Constructs <see cref="DoorLock"/> instances for the <see cref="DeviceFactory"/>.
/// </summary>
public sealed class DoorLockBuilder : IDeviceBuilder
{
    public DeviceType HandledType => DeviceType.DoorLock;

    public Device Build(string name, string location) => new DoorLock(name, location);
}