namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Defines the contract for a command that can be executed against a device.
/// Encapsulates a single operation as an object.
/// </summary>
public interface IDeviceCommand
{
    /// <summary>
    /// Executes the command.
    /// The receiver of the command is typically bound at creation time.
    /// </summary>
    void Execute();
}
