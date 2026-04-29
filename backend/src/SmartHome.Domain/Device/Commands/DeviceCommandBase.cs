using SmartHome.Domain.Device;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Base class for all device commands.
/// Provides common access to device metadata for structured results.
/// </summary>
public abstract class DeviceCommandBase : IDeviceCommand
{
    protected readonly Device Device;

    protected DeviceCommandBase(Device device)
    {
        Device = device;
    }

    public Guid? DeviceId => Device.Id;

    public string? DeviceName => Device.Name;

    public DeviceType? DeviceType => Device.Type;

    public abstract string OperationName { get; }

    public abstract string? Value { get; }

    public abstract CommandResult Execute();
}