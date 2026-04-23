using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Registration;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device.Commands;
using SmartHome.Domain.Common;
using SmartHome.Domain.Device.Events;
using SmartHome.Infrastructure.Device.Events;

namespace SmartHome.Infrastructure.Device.Service;

/// <summary>
/// Domain service for managing device lifecycle and high-level operations.
/// </summary>
/// <param name="repository">Persistence gateway for device entities.</param>
/// <param name="factory">Creates device instances from their underlying components.</param>
/// <param name="commandFactory">Factory for resolving device-specific commands.</param>
/// <param name="deviceEventPublisher">
/// Publishes runtime device-change events after persistence succeeds,
/// allowing the SSE broker to notify connected clients.
/// </param>
public sealed class DeviceService(
    IDeviceRepository repository,
    IDeviceFactory factory,
    IDeviceCommandFactory commandFactory,
    IDeviceEventPublisher deviceEventPublisher) : IDeviceService
{
    /// <inheritdoc />
    public async Task<Domain.Device.Device> RegisterDeviceAsync(
        string name,
        string location,
        DeviceType type,
        CancellationToken cancellationToken = default)
    {
        // One thermostat per location rule
        if (type == DeviceType.Thermostat &&
            await repository.ThermostatExistsAtLocationAsync(location, cancellationToken))
        {
            throw new DuplicateThermostatException(location);
        }

        // Create the concrete device via the factory
        var device = factory.Create(name, location, type);

        // Stage the new device for persistence.
        await repository.AddAsync(device, cancellationToken);
        
        // Commit new device to the database.
        await repository.SaveChangesAsync(cancellationToken);

        // Publish a Created event and hand off to the broker
        await deviceEventPublisher.PublishAsync(
            new DeviceChangedEvent(
                device.Id,
                DeviceChangeType.Created,
                DeviceEventPayloadFactory.Create(device)),
            cancellationToken);
        
        return device;
    }

    /// <inheritdoc />                      
    public async Task<Domain.Device.Device> ExecuteCommandAsync(
        Guid deviceId,
        string commandName,
        string? value,
        CancellationToken cancellationToken = default)
    {
        // Load the device.
        var device = await repository.GetByIdAsync(deviceId, cancellationToken);

        // Validate the devices exists.
        if (device is null)
        {
            throw new ResourceNotFoundException($"Device with id {deviceId} not found.");
        }

        // Normalize input.
        var commandValue = ValueParser.Normalize(value);
        
        // Create a command.
        var command = commandFactory.Create(commandName, commandValue, device);

        // Execute the command.
        command.Execute();

        // Log the action.
        await repository.LogActionAsync(device.Id, $"{commandName}: {commandValue}", cancellationToken);

        // Save changes to the database.
        await repository.SaveChangesAsync(cancellationToken);
        
        // Publish the Updated event and hand off to the broker.
        await deviceEventPublisher.PublishAsync(
            new DeviceChangedEvent(
                device.Id,
                DeviceChangeType.Updated,
                DeviceEventPayloadFactory.Create(device)),
            cancellationToken);
        
        return device;
    }
    
    /// <inheritdoc />
    /// <remarks>
    /// Removes a device from the system and publishes a <see cref="DeviceChangeType.Deleted"/>
    /// event after persistence succeeds.
    ///
    /// The device state is captured before deletion so a final snapshot can be included
    /// in the event payload. This allows clients to update or remove the device from
    /// their local state without requiring a full refresh.
    /// </remarks>
    public async Task RemoveDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        // Load the device first so we can capture its final snapshot for the Deleted event.
        var device = await repository.GetByIdAsync(deviceId, cancellationToken);

        if (device is null)
        {
            throw new ResourceNotFoundException($"Device with id {deviceId} not found.");
        }

        // Build payload before the device is removed from persistence.
        var payload = DeviceEventPayloadFactory.Create(device);

        var removed = await repository.RemoveByIdAsync(deviceId, cancellationToken);

        if (!removed)
        {
            throw new ResourceNotFoundException($"Device with id {deviceId} not found.");
        }

        await repository.SaveChangesAsync(cancellationToken);

        await deviceEventPublisher.PublishAsync(
            new DeviceChangedEvent(
                deviceId,
                DeviceChangeType.Deleted,
                payload),
            cancellationToken);
    }
}
