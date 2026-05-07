using SmartHome.Domain.Device.DoorLock;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Builds lock commands for lockable devices.
/// </summary>
public sealed class LockCommandBuilder : IDeviceCommandBuilder
{
    /// <inheritdoc />
    public string CommandName => "lock";

    /// <inheritdoc />
    public bool CanBuild(Device device) => device is ILockable;

    /// <inheritdoc />
    public IDeviceCommand Build(object? value, Device device)
    {
        return new LockCommand(
            (ILockable)device,
            device);
    }
}