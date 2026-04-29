namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Base class for all device commands.
/// Provides common access to device metadata for structured results.
/// Commands are always bound to a valid target device.
/// </summary>
public abstract class DeviceCommandBase : IDeviceCommand
{
    /// <summary>
    /// The device instance that this command operates on.
    /// </summary>
    protected readonly Device Device;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceCommandBase"/> class.
    /// </summary>
    /// <param name="device">The target device for the command.</param>
    protected DeviceCommandBase(Device device)
    {
        Device = device;
    }

    /// <summary>
    /// Gets the unique identifier of the target device.
    /// </summary>
    public Guid? DeviceId => Device.Id;

    /// <summary>
    /// Gets the human-readable name of the target device.
    /// </summary>
    public string? DeviceName => Device.Name;

    /// <summary>
    /// Gets the type of the target device.
    /// </summary>
    public DeviceType? DeviceType => Device.Type;

    /// <summary>
    /// Gets the operation name represented by this command (e.g., "SetBrightness", "Lock").
    /// </summary>
    public abstract string OperationName { get; }

    /// <summary>
    /// Gets the value associated with the command, if applicable.
    /// </summary>
    public abstract string? Value { get; }

    /// <summary>
    /// Executes the command against the target device.
    /// </summary>
    /// <returns>A <see cref="CommandResult"/> describing the outcome of execution.</returns>
    public abstract CommandResult Execute();
}