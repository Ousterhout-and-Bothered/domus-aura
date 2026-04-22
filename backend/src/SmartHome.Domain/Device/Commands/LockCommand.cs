using SmartHome.Domain.Device.DoorLock;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to lock a door lock device.
/// </summary>
/// <param name="receiver">The lockable device to operate on.</param>
public sealed class LockCommand(ILockable receiver) : IDeviceCommand
{
    /// <inheritdoc />
    public void Execute()
    {
        receiver.Lock();
    }
}
