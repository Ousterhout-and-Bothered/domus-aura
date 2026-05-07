using SmartHome.Domain.Device.Thermostat;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Builds desired temperature commands for thermostat-controllable devices.
/// </summary>
public sealed class SetDesiredTemperatureCommandBuilder : IDeviceCommandBuilder
{
    /// <inheritdoc />
    public string CommandName => "setdesiredtemperature";

    /// <inheritdoc />
    public bool CanBuild(Device device) => device is IThermostatControllable;

    /// <inheritdoc />
    public IDeviceCommand Build(object? value, Device device)
    {
        return new SetDesiredTemperatureCommand(
            (IThermostatControllable)device,
            CommandValueParser.ParseInt(value),
            device);
    }
}