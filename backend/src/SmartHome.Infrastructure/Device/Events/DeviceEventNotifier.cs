using SmartHome.Domain.Device.Events;
using DeviceModel = SmartHome.Domain.Device.Device;

namespace SmartHome.Infrastructure.Device.Events;

/// <summary>
/// Infrastructure implementation of <see cref="IDeviceEventNotifier"/> that
/// constructs device change events and publishes them via the configured
/// event publisher.
/// </summary>
/// <remarks>
/// For create and update operations, this class constructs event payloads
/// using <see cref="DeviceEventPayloadFactory"/>.
/// For delete operations, it accepts a precomputed payload to ensure the
/// removed device's final state can still be communicated to clients.
/// </remarks>
public sealed class DeviceEventNotifier(
    IDeviceEventPublisher publisher) : IDeviceEventNotifier
{
    
    /// <inheritdoc />
    public ValueTask PublishAsync(
        DeviceModel device,
        DeviceChangeType changeType,
        CancellationToken cancellationToken = default)
    {
        return publisher.PublishAsync(
            new DeviceChangedEvent(
                device.Id,
                changeType,
                DeviceEventPayloadFactory.Create(device)),
            cancellationToken);
    }
    /// <inheritdoc />
    public ValueTask PublishDeletedAsync(
        Guid deviceId,
        object payload,
        CancellationToken cancellationToken = default)
    {
        return publisher.PublishAsync(
            new DeviceChangedEvent(
                deviceId,
                DeviceChangeType.Deleted,
                payload),
            cancellationToken);
    }
}