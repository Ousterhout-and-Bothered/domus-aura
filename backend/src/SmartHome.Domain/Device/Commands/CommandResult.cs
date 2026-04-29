namespace SmartHome.Domain.Device.Commands;

public sealed record CommandResult(
    Guid DeviceId,
    string DeviceName,
    DeviceType DeviceType,
    string Operation,
    string? Value,
    bool Success,
    string? Message);