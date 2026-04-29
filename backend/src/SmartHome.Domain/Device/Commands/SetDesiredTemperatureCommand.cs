using SmartHome.Domain.Device.Thermostat;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to set the desired temperature of a thermostat.
/// </summary>
public sealed class SetDesiredTemperatureCommand(
    IThermostatControllable receiver,
    int temperature,
    Device device) : DeviceCommandBase(device)
{
    public override string OperationName => "SetDesiredTemperature";

    public override string Value => temperature.ToString();

    /// <inheritdoc />
    public override CommandResult Execute()
    {
        receiver.SetDesiredTemperature(temperature);

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