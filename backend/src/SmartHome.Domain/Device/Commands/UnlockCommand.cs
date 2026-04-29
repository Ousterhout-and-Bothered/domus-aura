using SmartHome.Domain.Device.DoorLock;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to unlock a door lock device.
/// </summary>
public sealed class UnlockCommand(
    ILockable receiver,
    Device device) : DeviceCommandBase(device)
{
    public override string OperationName => "Unlock";

    public override string? Value => null;

    /// <inheritdoc />
    public override CommandResult Execute()
    {
        receiver.Unlock();

        return new CommandResult(
            DeviceId: DeviceId!.Value,
            DeviceName: DeviceName!,
            DeviceType: DeviceType!.Value,
            Operation: OperationName,
            Value: Value,
            Success: true,
            Message: null);
    }
}
