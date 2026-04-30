using SmartHome.Domain.Device.Light;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Represents a device command that sets the brightness of a dimmable device.
/// </summary>
/// <remarks>
/// The command delegates brightness validation and state rules to the
/// <see cref="IDimmable"/> receiver. If the receiver is already at the requested
/// brightness, the command still executes successfully but reports the result as
/// a no-op.
/// </remarks>
public sealed class SetBrightnessCommand(
    IDimmable receiver,
    int brightness,
    Device device) : DeviceCommandBase(device)
{
    /// <summary>
    /// Gets the operation name recorded for this command.
    /// </summary>
    public override string OperationName => "SetBrightness";

    /// <summary>
    /// Gets the requested brightness value as a string for command history and scene results.
    /// </summary>
    public override string Value => brightness.ToString();

    /// <inheritdoc />
    public override CommandResult Execute()
    {
        var wasAlreadyInRequestedState = receiver.Brightness == brightness;

        receiver.SetBrightness(brightness);

        return new CommandResult(
            DeviceId: DeviceId!.Value,
            DeviceName: DeviceName!,
            DeviceType: DeviceType!.Value,
            Operation: OperationName,
            Value: Value,
            Success: true,
            Message: wasAlreadyInRequestedState
                ? "Device is already in the requested state."
                : null,
            IsNoOp: wasAlreadyInRequestedState);
    }
}