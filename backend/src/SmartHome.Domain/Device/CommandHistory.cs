namespace SmartHome.Domain.Device;

/// <summary>
/// Represents a record of a single command operation performed on a device.
/// Stores a snapshot of the device metadata at the time of the operation
/// so history remains readable even if the device is later removed.
/// </summary>
public sealed class CommandHistory
{
    /// <summary>
    /// Gets the unique identifier for this history record.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the device the operation was performed on.
    /// </summary>
    public Guid DeviceId { get; private set; }

    /// <summary>
    /// Gets the device name at the time the operation occurred.
    /// </summary>
    public string DeviceName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the device location at the time the operation occurred.
    /// </summary>
    public string DeviceLocation { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the device type at the time the operation occurred.
    /// </summary>
    public string DeviceType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a human-readable description of the operation.
    /// </summary>
    public string Operation { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the exact point in time when the operation occurred.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    // Required for EF Core.
    private CommandHistory() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandHistory"/> class.
    /// Captures a snapshot of the device metadata and sets the timestamp automatically.
    /// </summary>
    /// <param name="deviceId">
    /// The unique identifier of the device associated with the operation.
    /// </param>
    /// <param name="deviceName">
    /// The device name at the time the operation occurred.
    /// </param>
    /// <param name="deviceLocation">
    /// The device location at the time the operation occurred.
    /// </param>
    /// <param name="deviceType">
    /// The device type at the time the operation occurred.
    /// </param>
    /// <param name="operation">
    /// A human-readable description of the operation performed.
    /// </param>
    public CommandHistory(
        Guid deviceId,
        string deviceName,
        string deviceLocation,
        string deviceType,
        string operation)
    {
        Id = Guid.NewGuid();
        DeviceId = deviceId;
        DeviceName = deviceName;
        DeviceLocation = deviceLocation;
        DeviceType = deviceType;
        Operation = operation;
        Timestamp = DateTime.UtcNow;
    }
}