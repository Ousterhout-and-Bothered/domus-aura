using SmartHome.Domain.Device.DoorLock;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Builds unlock commands for lockable devices.
/// </summary>
public sealed class UnlockCommandBuilder : IDeviceCommandBuilder
{
    /// <inheritdoc />
    public string CommandName => "unlock";

    /// <inheritdoc />
    public bool CanBuild(Device device) => device is ILockable;

    /// <inheritdoc />
    public IDeviceCommand Build(object? value, Device device)
    {
        return new UnlockCommand(
            (ILockable)device,
            device);
    }
}