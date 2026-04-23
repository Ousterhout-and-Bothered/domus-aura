namespace SmartHome.Domain.Device.Events;

public sealed record DeviceChangedEvent(
    Guid DeviceId,
    DeviceChangeType ChangeType,
    object? Payload = null
);