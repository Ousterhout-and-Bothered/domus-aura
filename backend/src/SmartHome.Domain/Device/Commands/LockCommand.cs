using SmartHome.Domain.Device.DoorLock;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to lock a door lock device.
/// </summary>
public sealed class LockCommand(
    ILockable receiver,
    Device device) : DeviceCommandBase(device)
{
    public override string OperationName => "Lock";

    public override string? Value => null;

    /// <inheritdoc />
    public override CommandResult Execute()
    {
        receiver.Lock();

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