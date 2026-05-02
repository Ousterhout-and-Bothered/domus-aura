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
        var thermostat = (Thermostat.Thermostat)Device;

        var wasOff = receiver.State == ThermostatState.Off;
        var wasNotAuto = thermostat.Mode != ThermostatMode.Auto;
        var tempAlreadyAtTarget = thermostat.DesiredTemperature == temperature;

        // Step 1: ensure non-Off state. SetMode below requires it.
        if (wasOff)
        {
            receiver.TurnOn();
        }

        // Step 2: ensure Auto mode. Only Auto guarantees the system will
        // actively reach the target regardless of whether ambient is above
        // or below it. Heat-only and Cool-only modes are user preferences
        // for direct device control; a scene-authored temperature target
        // overrides them in service of the scene's stated intent.
        if (wasNotAuto)
        {
            receiver.SetMode(ThermostatMode.Auto);
        }

        // Step 3: apply the temperature.
        receiver.SetDesiredTemperature(temperature);

        // A no-op is *only* a no-op if nothing else changed either. An
        // implicit power-on or mode change is itself a state transition.
        var isNoOp = tempAlreadyAtTarget && !wasOff && !wasNotAuto;

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
            ImplicitPowerOn: wasOff,
            ImplicitModeChange: wasNotAuto);
    }
}