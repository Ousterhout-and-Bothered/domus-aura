using SmartHome.Domain.Device.Thermostat;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to set the power state of a powered device.
/// </summary>
/// <remarks>
/// For most powered devices, the requested state is compared directly against
/// <see cref="IPowerable.PowerState"/>. Thermostats are handled specially because
/// their active states are represented by <see cref="ThermostatState"/> values:
/// any thermostat state other than <see cref="ThermostatState.Off"/> is treated
/// as already powered on.
/// </remarks>
public sealed class SetPowerCommand(
    IPowerable receiver,
    PowerState targetState,
    Device device) : DeviceCommandBase(device)
{
    /// <summary>
    /// Gets the operation name recorded for this command.
    /// </summary>
    public override string OperationName => "SetPower";

    /// <summary>
    /// Gets the requested power state as a string for command history and scene results.
    /// </summary>
    public override string Value => targetState.ToString();

    /// <inheritdoc />
    public override CommandResult Execute()
    {
        var wasAlreadyInRequestedState =
            Device is SmartHome.Domain.Device.Thermostat.Thermostat thermostat
                ? (targetState == PowerState.On
                    ? thermostat.State != ThermostatState.Off
                    : thermostat.State == ThermostatState.Off)
                : receiver.PowerState == targetState;
    
        if (targetState == PowerState.On)
        {
            receiver.TurnOn();
        }
        else
        {
            receiver.TurnOff();
        }

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