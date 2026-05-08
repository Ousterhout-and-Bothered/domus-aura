namespace SmartHome.Domain.Device;

/// <summary>
/// Represents a record of a single command operation performed on a device.
/// </summary>
public sealed class CommandHistory
{
    /// <summary>
    /// Unique identifier for this history record.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The unique identifier of the device the operation was performed on.
    /// </summary>
    public Guid DeviceId { get; private set; }

    /// <summary>
    /// A human-readable description of the operation.
    /// </summary>
    public string Operation { get; private set; } = string.Empty;

    /// <summary>
    /// The exact point in time when the operation occurred.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    // Required for EF Core.
    private CommandHistory() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandHistory"/> class.
    /// Sets a unique ID and the current UTC timestamp automatically.
    /// </summary>
    /// <param name="deviceId">The device being operated on.</param>
    /// <param name="operation">Description of what happened.</param>
    public CommandHistory(Guid deviceId, string operation)
    {
        Id = Guid.NewGuid();
        DeviceId = deviceId;
        Operation = operation;
        Timestamp = DateTime.UtcNow;
    }
}
