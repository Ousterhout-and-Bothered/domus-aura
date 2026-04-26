namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// An <see cref="IDeviceCommand"/> that represents a command which failed before execution
/// — typically because command construction itself was invalid (e.g. an incompatible op/device
/// pairing caught by the factory). Executing it returns the pre-baked failure result.
/// </summary>
public sealed class FailedCommand(string operationName, string message) : IDeviceCommand
{
    /// <inheritdoc />
    public string OperationName { get; } = operationName;

    /// <summary>The reason this command could not be constructed.</summary>
    public string Message { get; } = message;

    /// <inheritdoc />
    public CommandResult Execute() =>
        new(Operation: OperationName, Success: false, Message: Message);
}