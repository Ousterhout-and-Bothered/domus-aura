namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Defines the contract for resolving device command names into executable command objects.
/// </summary>
public interface IDeviceCommandFactory
{
    /// <summary>
    /// Creates an <see cref="IDeviceCommand"/> from a command name and optional value, 
    /// ensuring it is compatible with the target device.
    /// </summary>
    /// <param name="commandName">The name of the command to create (e.g., "SetPower", "Lock").</param>
    /// <param name="value">The optional payload for the command (e.g., brightness level, target state).</param>
    /// <param name="device">The target device the command will be executed against.</param>
    /// <returns>An executable <see cref="IDeviceCommand"/>.</returns>
    IDeviceCommand Create(string commandName, object? value, Device device);
}
