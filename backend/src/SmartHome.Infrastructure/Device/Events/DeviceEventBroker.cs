using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using SmartHome.Domain.Device.Events;

namespace SmartHome.Infrastructure.Device.Events;

/// <summary>
/// In-memory event broker that broadcasts device change events to all active subscribers.
/// Each subscriber receives its own bounded channel so SSE clients can consume events independently
/// without allowing unbounded memory growth if a client falls behind.
/// </summary>
public sealed class DeviceEventBroker : IDeviceEventPublisher, IDeviceEventStream
{
    private readonly ConcurrentDictionary<Guid, Channel<DeviceChangedEvent>> _subscribers = new();

    /// <inheritdoc />
    public ValueTask PublishAsync(
        DeviceChangedEvent deviceEvent,
        CancellationToken cancellationToken = default)
    {
        foreach (var subscriber in _subscribers.Values)
        {
            subscriber.Writer.TryWrite(deviceEvent);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<DeviceChangedEvent> SubscribeAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subscriberId = Guid.NewGuid();

        var channel = Channel.CreateBounded<DeviceChangedEvent>(new BoundedChannelOptions(100)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        _subscribers[subscriberId] = channel;

        try
        {
            await foreach (var deviceEvent in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return deviceEvent;
            }
        }
        finally
        {
            _subscribers.TryRemove(subscriberId, out _);
            channel.Writer.TryComplete();
        }
    }
}