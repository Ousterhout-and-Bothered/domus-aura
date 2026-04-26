namespace SmartHome.Domain.Device.Commands;

public sealed record CommandResult(
    string Operation,
    bool Success,
    string? Message = null);