using SmartHome.Domain.Device.Thermostat;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Builds thermostat mode commands for thermostat-controllable devices.
/// </summary>
public sealed class SetModeCommandBuilder : IDeviceCommandBuilder
{
    /// <inheritdoc />
    public string CommandName => "setmode";

    /// <inheritdoc />
    public bool CanBuild(Device device) => device is IThermostatControllable;

    /// <inheritdoc />
    public IDeviceCommand Build(object? value, Device device)
    {
        return new SetModeCommand(
            (IThermostatControllable)device,
            CommandValueParser.ParseEnum<ThermostatMode>(value),
            device);
    }
}