using SmartHome.Domain.Device.Thermostat;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to set the desired temperature of a thermostat.
/// </summary>
/// <remarks>
/// This command applies only to thermostat devices. The current desired temperature
/// is compared to the requested value to determine whether the operation results
/// in a state change or a no-op. The command requires the thermostat to be powered on.
/// </remarks>
public sealed class SetDesiredTemperatureCommand(
    IThermostatControllable receiver,
    int temperature,
    Device device) : DeviceCommandBase(device)
{
    /// <summary>
    /// Gets the operation name recorded for this command.
    /// </summary>
    public override string OperationName => "SetDesiredTemperature";

    /// <summary>
    /// Gets the requested temperature as a string for command history and scene results.
    /// </summary>
    public override string Value => temperature.ToString();

    /// <inheritdoc />
    public override CommandResult Execute()
    {
        var thermostat = (SmartHome.Domain.Device.Thermostat.Thermostat)Device;

        var wasAlreadyInRequestedState = thermostat.DesiredTemperature == temperature;

        receiver.SetDesiredTemperature(temperature);

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