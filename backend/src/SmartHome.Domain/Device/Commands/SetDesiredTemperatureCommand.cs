using SmartHome.Domain.Device.Thermostat;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to set the desired temperature of a thermostat.
/// </summary>
/// <param name="receiver">The thermostat to operate on.</param>
/// <param name="temperature">The target temperature value.</param>
public sealed class SetDesiredTemperatureCommand(IThermostatControllable receiver, int temperature) : IDeviceCommand
{
    /// <inheritdoc />
    public void Execute()
    {
        receiver.SetDesiredTemperature(temperature);
    }
}
