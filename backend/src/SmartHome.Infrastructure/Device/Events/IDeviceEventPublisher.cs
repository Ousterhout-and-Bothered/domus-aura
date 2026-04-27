using SmartHome.Domain.Device.Events;

namespace SmartHome.Infrastructure.Device.Events;

/// <summary>
/// Defines a contract for publishing device change events to interested subscribers.
/// </summary>
/// <remarks>
/// Implementations are responsible for delivering <see cref="DeviceChangedEvent"/> instances
/// to all active listeners, such as SSE clients consuming the event stream.
/// </remarks>
public interface IDeviceEventPublisher
{
    /// <summary>
    /// Publishes a device change event to all subscribers.
    /// </summary>
    /// <param name="deviceEvent">
    /// The event describing the device change, including its identifier, change type,
    /// and optional payload snapshot.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to observe cancellation of the publish operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous publish operation.
    /// </returns>
    ValueTask PublishAsync(
        DeviceChangedEvent deviceEvent,
        CancellationToken cancellationToken = default);
}