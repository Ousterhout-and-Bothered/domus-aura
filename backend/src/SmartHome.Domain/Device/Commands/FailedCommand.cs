namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// An <see cref="IDeviceCommand"/> that represents a command which failed before execution
/// — typically because command construction itself was invalid.
/// Executing it returns the pre-baked failure result.
/// </summary>
public sealed class FailedCommand(
    string operationName,
    string message,
    Guid? deviceId = null,
    string? deviceName = null,
    DeviceType? deviceType = null,
    string? value = null) : IDeviceCommand
{
    /// <inheritdoc />
    public Guid? DeviceId { get; } = deviceId;

    /// <inheritdoc />
    public string? DeviceName { get; } = deviceName;

    /// <inheritdoc />
    public DeviceType? DeviceType { get; } = deviceType;

    /// <inheritdoc />
    public string OperationName { get; } = operationName;

    /// <inheritdoc />
    public string? Value { get; } = value;

    /// <summary>The reason this command could not be constructed.</summary>
    public string Message { get; } = message;

    /// <inheritdoc />
    public CommandResult Execute() =>
        new(
            DeviceId: DeviceId ?? Guid.Empty,
            DeviceName: DeviceName ?? "Unknown",
            DeviceType: DeviceType ?? default,
            Operation: OperationName,
            Value: Value,
            Success: false,
            Message: Message);
}