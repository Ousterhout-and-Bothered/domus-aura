using SmartHome.Domain.Device.Fan;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to set the speed of a fan device.
/// </summary>
public sealed class SetSpeedCommand(
    IFanControllable receiver,
    FanSpeed speed,
    Device device) : DeviceCommandBase(device)
{
    public override string OperationName => "SetSpeed";

    public override string Value => speed.ToString();

    /// <inheritdoc />
    public override CommandResult Execute()
    {
        receiver.SetSpeed(speed);

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