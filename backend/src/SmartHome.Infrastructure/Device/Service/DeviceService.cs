using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Registration;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Device.Events;
using SmartHome.Domain.Device.Commands;
using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Common;
using SmartHome.Domain.Scene;
using SmartHome.Infrastructure.Device.Events;

namespace SmartHome.Infrastructure.Device.Service;

/// <summary>
/// Domain service for managing device lifecycle and high-level operations.
/// </summary>
/// <param name="repository">Persistence gateway for device entities.</param>
/// <param name="factory">Creates device instances from their underlying components.</param>
/// <param name="commandFactory">Factory for resolving device-specific commands.</param>
/// <param name="deviceEventNotifier">
/// Triggers device-change notifications after persistence succeeds,
/// allowing the SSE broker to notify connected clients.
/// </param>
public sealed class DeviceService(
    IDeviceRepository repository,
    IDeviceFactory factory,
    IDeviceCommandFactory commandFactory,
    IDeviceEventNotifier deviceEventNotifier,
    ISceneRepository sceneRepository) : IDeviceService
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
        
        var device = factory.Create(name, location, type);
        
        await repository.AddAsync(device, cancellationToken);
        
        await repository.SaveChangesAsync(cancellationToken);
        
        await deviceEventNotifier.PublishAsync(
            device,
            DeviceChangeType.Created,
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
        var device = await repository.GetByIdAsync(deviceId, cancellationToken);
        
        if (device is null)
        {
            throw new ResourceNotFoundException($"Device with id {deviceId} not found.");
        }
        
        var commandValue = ValueParser.Normalize(value);
        
        var command = commandFactory.Create(commandName, commandValue, device);
        
        command.Execute();
        
        await repository.LogActionAsync(device.Id, $"{commandName}: {commandValue}", cancellationToken);
        
        await repository.SaveChangesAsync(cancellationToken);
        
        await deviceEventNotifier.PublishAsync(
            device,
            DeviceChangeType.Updated,
            cancellationToken);
        
        return device;
    }
    
    /// <inheritdoc />
    /// <remarks>
    /// Removes a device from the system and publishes a <see cref="DeviceChangeType.Deleted"/>
    /// event only after the deletion succeeds.
    ///
    /// The device payload is captured before deletion so the final event can include
    /// the removed device's last known state.
    ///
    /// If the device is deleted between the snapshot read and the delete operation,
    /// the method throws <see cref="ResourceNotFoundException"/> and does not publish
    /// a deletion event.
    /// </remarks>
    public async Task RemoveDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        var device = await repository.GetByIdReadOnlyAsync(deviceId, cancellationToken);

        if (device is null)
        {
            throw new ResourceNotFoundException($"Device with id {deviceId} not found.");
        }

        var payload = DeviceEventPayloadFactory.Create(device);

        var affectedScenes = await sceneRepository
            .RemoveActionsForDeviceAsync(deviceId, cancellationToken);

        if (affectedScenes.Count > 0)
        {
            var sceneList = string.Join(", ", affectedScenes);

            await repository.LogActionAsync(
                deviceId,
                $"Scene cleanup: removed device references from affected scenes [{sceneList}]",
                cancellationToken);
        }

        var removed = await repository.RemoveByIdAsync(deviceId, cancellationToken);

        if (!removed)
        {
            throw new ResourceNotFoundException($"Device with id {deviceId} not found.");
        }

        await repository.SaveChangesAsync(cancellationToken);

        await deviceEventNotifier.PublishDeletedAsync(
            deviceId,
            payload,
            cancellationToken);
    }
    
    /// <inheritdoc />
    /// Ensures controllers do not access the repository directly.
    public async Task<IReadOnlyList<Domain.Device.Device>> GetAllDevicesAsync(
        string? location,
        DeviceType? type,
        bool? isOn,
        CancellationToken cancellationToken = default)
    {
        return await repository.GetAllAsync(location, type, isOn, cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<Domain.Device.Device> GetDeviceByIdAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        var device = await repository.GetByIdAsync(deviceId, cancellationToken);

        if (device is null)
        {
            throw new ResourceNotFoundException($"Device with id {deviceId} not found.");
        }

        return device;
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<CommandHistory>> GetDeviceHistoryAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        var device = await repository.GetByIdAsync(deviceId, cancellationToken);

        if (device is null)
        {
            throw new ResourceNotFoundException($"Device with id {deviceId} not found.");
        }

        return await repository.GetHistoryAsync(deviceId, cancellationToken);
    }
}
