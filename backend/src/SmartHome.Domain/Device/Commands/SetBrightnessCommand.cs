using SmartHome.Domain.Device.Light;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to set the brightness of a dimmable device.
/// </summary>
public sealed class SetBrightnessCommand(
    IDimmable receiver,
    int brightness,
    Device device) : DeviceCommandBase(device)
{
    public override string OperationName => "SetBrightness";

    public override string Value => brightness.ToString();

    /// <inheritdoc />
    public override CommandResult Execute()
    {
        receiver.SetBrightness(brightness);

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