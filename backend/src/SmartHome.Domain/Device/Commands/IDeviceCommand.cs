namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Defines the contract for a command that can be executed against a device.
/// Encapsulates a single operation as an object.
/// </summary>
public interface IDeviceCommand
{
    
    /// <summary>
    /// Human-readable label for this command, matching the format used in
    /// <see cref="CommandResult.Operation"/> on success. Exposed separately
    /// so callers (e.g., scene execution) can identify the operation even
    /// when <see cref="Execute"/> throws before returning a result.
    /// </summary>
    string OperationName { get; }
    
    
    /// <summary>
    /// Executes the command.
    /// The receiver of the command is typically bound at creation time.
    /// </summary>
    CommandResult Execute();
}
