
using SmartHome.Domain.Device.Light;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to set the color of a colorable device.
/// </summary>
public sealed class SetColorCommand(
    IColorable receiver,
    IPowerable power,
    string colorHex,
    Device device) : DeviceCommandBase(device)
{
    public override string OperationName => "SetColor";

    public override string Value => colorHex;

    /// <inheritdoc />
    public override CommandResult Execute()
    {
        var wasOff = power.PowerState == PowerState.Off;

        // Color comparison is case-insensitive — the device stores upper-case,
        // but a user-supplied value may be lower or mixed case.
        var colorAlreadyAtTarget = string.Equals(
            receiver.ColorHex,
            colorHex,
            StringComparison.OrdinalIgnoreCase);

        if (wasOff)
        {
            power.TurnOn();
        }

        receiver.SetColor(colorHex);

        var isNoOp = colorAlreadyAtTarget && !wasOff;

        return new CommandResult(
            DeviceId: DeviceId!.Value,
            DeviceName: DeviceName!,
            DeviceType: DeviceType!.Value,
            Operation: OperationName,
            Value: Value,
            Success: true,
            Message: isNoOp
                ? "Device is already in the requested state."
                : null,
            IsNoOp: isNoOp,
            ImplicitPowerOn: wasOff);
    }
}