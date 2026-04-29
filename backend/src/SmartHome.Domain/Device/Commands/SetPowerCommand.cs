namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to toggle the power state of a device.
/// </summary>
public sealed class SetPowerCommand(
    IPowerable receiver,
    PowerState targetState,
    Device device) : DeviceCommandBase(device)
{
    public override string OperationName => "SetPower";

    public override string Value => targetState.ToString();

    /// <inheritdoc />
    public override CommandResult Execute()
    {
        if (targetState == PowerState.On)
            receiver.TurnOn();
        else
            receiver.TurnOff();

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
