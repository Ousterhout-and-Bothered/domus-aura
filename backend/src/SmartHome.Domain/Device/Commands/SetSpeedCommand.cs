using SmartHome.Domain.Device.Fan;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to set the speed of a fan device.
/// </summary>
/// <remarks>
/// This command applies only to devices that implement <see cref="IFanControllable"/>.
/// The requested speed is compared to the current speed to determine whether the
/// operation results in a state change or a no-op.
/// </remarks>
public sealed class SetSpeedCommand(
    IFanControllable receiver,
    IPowerable power,
    FanSpeed speed,
    Device device) : DeviceCommandBase(device)
{
    /// <summary>
    /// Gets the operation name recorded for this command.
    /// </summary>
    public override string OperationName => "SetSpeed";

    /// <summary>
    /// Gets the requested fan speed as a string for command history and scene results.
    /// </summary>
    public override string Value => speed.ToString();

    /// <inheritdoc />
    public override CommandResult Execute()
    {
        var wasOff = power.PowerState == PowerState.Off;
        var speedAlreadyAtTarget = receiver.Speed == speed;

        if (wasOff)
        {
            power.TurnOn();
        }

        receiver.SetSpeed(speed);

        var isNoOp = speedAlreadyAtTarget && !wasOff;

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