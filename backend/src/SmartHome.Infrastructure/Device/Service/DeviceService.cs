using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Registration;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device.Commands;
using SmartHome.Domain.Common;

namespace SmartHome.Infrastructure.Device.Service;

/// <summary>
/// Domain service for managing device lifecycle and high-level operations.
/// </summary>
/// <param name="repository">Persistence gateway for device entities.</param>
/// <param name="factory">Creates device instances from their underlying components.</param>
/// <param name="commandFactory">Factory for resolving device-specific commands.</param>
public sealed class DeviceService(
    IDeviceRepository repository,
    IDeviceFactory factory,
    IDeviceCommandFactory commandFactory) : IDeviceService
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

        // Add to persistence
        await repository.AddAsync(device, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

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
            throw new KeyNotFoundException($"Device with id {deviceId} not found.");
        }

        var commandValue = ValueParser.Normalize(value);
        var command = commandFactory.Create(commandName, commandValue, device);

        command.Execute();

        await repository.LogActionAsync(device.Id, $"{commandName}: {commandValue}", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return device;
    }
}
