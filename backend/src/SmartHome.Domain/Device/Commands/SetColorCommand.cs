
using SmartHome.Domain.Device.Light;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to set the color of a colorable device.
/// </summary>
public sealed class SetColorCommand(
    IColorable receiver,
    string colorHex,
    Device device) : DeviceCommandBase(device)
{
    public override string OperationName => "SetColor";

    public override string Value => colorHex;

    /// <inheritdoc />
    public override CommandResult Execute()
    {
        receiver.SetColor(colorHex);

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