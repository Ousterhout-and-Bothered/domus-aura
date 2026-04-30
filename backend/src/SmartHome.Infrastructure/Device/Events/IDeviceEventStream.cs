using SmartHome.Domain.Device.Events;

namespace SmartHome.Infrastructure.Device.Events;

/// <summary>
/// Defines a contract for subscribing to a stream of device change events.
/// </summary>
/// <remarks>
/// Implementations provide a continuous asynchronous stream of
/// <see cref="DeviceChangedEvent"/> instances, typically consumed by
/// SSE endpoints to push real-time updates to connected clients.
/// </remarks>
public interface IDeviceEventStream
{
    /// <summary>
    /// Subscribes to the device event stream.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token used to cancel the subscription and stop receiving events.
    /// </param>
    /// <returns>
    /// An <see cref="IAsyncEnumerable{DeviceChangedEvent}"/> that yields events
    /// as they occur.
    /// </returns>
    IAsyncEnumerable<DeviceChangedEvent> SubscribeAsync(CancellationToken cancellationToken = default);
}