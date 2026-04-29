namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Defines the contract for a command that can be executed against a device.
/// Encapsulates a single operation as an object.
/// </summary>
public interface IDeviceCommand
{
    /// <summary>The ID of the target device, when known.</summary>
    Guid? DeviceId { get; }

    /// <summary>The name of the target device, when known.</summary>
    string? DeviceName { get; }

    /// <summary>The type of the target device, when known.</summary>
    DeviceType? DeviceType { get; }

    /// <summary>
    /// Human-readable label for this command. This should be the operation name only,
    /// not a formatted operation/value string.
    /// </summary>
    string OperationName { get; }

    /// <summary>The command value, when the operation has one.</summary>
    string? Value { get; }

    /// <summary>
    /// Executes the command.
    /// The receiver of the command is typically bound at creation time.
    /// </summary>
    CommandResult Execute();
}