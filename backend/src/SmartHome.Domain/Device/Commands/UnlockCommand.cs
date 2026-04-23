using SmartHome.Domain.Device.DoorLock;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to unlock a door lock device.
/// </summary>
/// <param name="receiver">The lockable device to operate on.</param>
public sealed class UnlockCommand(ILockable receiver) : IDeviceCommand
{
    
    public string OperationName => "Unlock";
    
    /// <inheritdoc />
    public CommandResult Execute()
    {
        receiver.Unlock();
        return new CommandResult(OperationName, true);
    }
}
