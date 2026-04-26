using SmartHome.Domain.Device.Thermostat;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to set the operating mode of a thermostat.
/// </summary>
/// <param name="receiver">The thermostat to operate on.</param>
/// <param name="mode">The target operating mode (e.g., Heat, Cool, Auto).</param>
public sealed class SetModeCommand(IThermostatControllable receiver, ThermostatMode mode) : IDeviceCommand
{
    
    public string OperationName => $"SetMode({mode})";
    
    /// <inheritdoc />
    public CommandResult Execute()
    {
        receiver.SetMode(mode);
        return new CommandResult(OperationName, true);
    }
}
