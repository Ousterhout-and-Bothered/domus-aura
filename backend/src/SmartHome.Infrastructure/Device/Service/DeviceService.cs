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

        await repository.LogActionAsync(
            device.Id,
            $"Registered: {type} at '{location}'",
            cancellationToken);

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

        // Log the removal itself, before deleting. The scene cleanup entry (if any)
        // is logged alongside in the same transaction so both are persisted as a
        // unit; the actual ExecuteDeleteAsync below runs as a separate SQL DELETE.
        await repository.LogActionAsync(
            deviceId,
            $"Removed: {device.Type} '{device.Name}' from '{device.Location}'",
            cancellationToken);

        if (affectedScenes.Count > 0)
        {
            var sceneList = string.Join(", ", affectedScenes);

            await repository.LogActionAsync(
                deviceId,
                $"Scene cleanup: removed device references from affected scenes [{sceneList}]",
                cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);

        var removed = await repository.RemoveByIdAsync(deviceId, cancellationToken);

        if (!removed)
        {
            throw new ResourceNotFoundException($"Device with id {deviceId} not found.");
        }

        await deviceEventNotifier.PublishDeletedAsync(
            deviceId,
            payload,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Domain.Device.Device> UpdateDeviceAsync(
        Guid deviceId,
        string name,
        string location,
        CancellationToken cancellationToken = default)
    {
        var device = await repository.GetByIdAsync(deviceId, cancellationToken);

        if (device is null)
        {
            throw new ResourceNotFoundException($"Device with id {deviceId} not found.");
        }

        var oldName = device.Name;
        var oldLocation = device.Location;

        var nameChanged = !string.Equals(oldName, name, StringComparison.Ordinal);
        var locationChanged = !string.Equals(oldLocation, location, StringComparison.Ordinal);

        // No-op short-circuit: nothing to persist, log, or broadcast.
        if (!nameChanged && !locationChanged)
        {
            return device;
        }

        // Thermostat-per-location invariant. Only enforced when the location actually
        // changes — a thermostat being renamed in place must not trip its own existence check.
        if (locationChanged && device.Type == DeviceType.Thermostat &&
            await repository.ThermostatExistsAtLocationAsync(location, cancellationToken))
        {
            throw new DuplicateThermostatException(location);
        }

        if (nameChanged)
        {
            device.Rename(name);
        }

        if (locationChanged)
        {
            device.Relocate(location);
        }

        var operation = BuildUpdateOperation(oldName, oldLocation, device);

        await repository.LogActionAsync(device.Id, operation, cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);

        await deviceEventNotifier.PublishAsync(
            device,
            DeviceChangeType.Updated,
            cancellationToken);

        return device;
    }

    // Composes the audit log entry for an update, including only the fields that changed.
    private static string BuildUpdateOperation(string oldName, string oldLocation, Domain.Device.Device device)
    {
        var parts = new List<string>(2);

        if (!string.Equals(oldName, device.Name, StringComparison.Ordinal))
        {
            parts.Add($"name '{oldName}' \u2192 '{device.Name}'");
        }

        if (!string.Equals(oldLocation, device.Location, StringComparison.Ordinal))
        {
            parts.Add($"location '{oldLocation}' \u2192 '{device.Location}'");
        }

        return $"Updated: {string.Join(", ", parts)}";
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

    /// <inheritdoc />
    public async Task<PagedResult<CommandHistory>> GetAllHistoryAsync(
        int page,
        int pageSize,
        string? location = null,
        Guid? deviceId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        return await repository.GetAllHistoryAsync(
            page, pageSize, location, deviceId, from, to, cancellationToken);
    }
}
