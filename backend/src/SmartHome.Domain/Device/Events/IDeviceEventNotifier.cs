using DeviceModel = SmartHome.Domain.Device.Device;

namespace SmartHome.Domain.Device.Events;

/// <summary>
/// Defines a contract for publishing device change notifications to downstream
/// consumers (e.g., SSE clients) without exposing event construction details
/// to application services.
/// </summary>
/// <remarks>
/// Implementations are responsible for constructing event payloads for create/update
/// operations. For delete operations, the payload must be supplied by the caller
/// because the device state is no longer available after removal.
/// </remarks>
public interface IDeviceEventNotifier
{
    
    /// <summary>
    /// Publishes a device change notification for the specified device.
    /// Used for create and update operations.
    /// </summary>
    ValueTask PublishAsync(
        DeviceModel device,
        DeviceChangeType changeType,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publishes a deletion notification using a precomputed payload.
    /// </summary>
    ValueTask PublishDeletedAsync(
        Guid deviceId,
        object payload,
        CancellationToken cancellationToken = default);
}