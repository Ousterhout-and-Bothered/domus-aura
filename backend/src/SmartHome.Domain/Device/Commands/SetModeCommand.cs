using SmartHome.Domain.Device.Thermostat;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to set the operating mode of a thermostat.
/// </summary>
public sealed class SetModeCommand(
    IThermostatControllable receiver,
    ThermostatMode mode,
    Device device) : DeviceCommandBase(device)
{
    public override string OperationName => "SetMode";

    public override string Value => mode.ToString();

    /// <inheritdoc />
    public override CommandResult Execute()
    {
        receiver.SetMode(mode);

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
