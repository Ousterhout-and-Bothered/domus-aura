using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Device;
using DomainDevice = SmartHome.Domain.Device.Device;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Simulation;
using SmartHome.Infrastructure.Persistence;
using ThermostatDevice = SmartHome.Domain.Device.Thermostat.Thermostat;

namespace SmartHome.Infrastructure.Device.Repository;

/// <summary>
/// EF Core implementation of the device repository contract.
/// Responsible for persistence concerns for devices.
/// </summary>
public sealed class DeviceRepository(SmartHomeDbContext dbContext) 
    : IDeviceRepository, ISimulationRepository
{
    
    /// <inheritdoc />
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => dbContext.Devices.AnyAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DomainDevice>> GetAllAsync(
        string? location = null, 
        DeviceType? type = null, 
        bool? isOn = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Devices.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(location))
        {
            query = query.Where(d => d.Location == location);
        }

        if (type.HasValue)
        {
            query = query.Where(d => d.Type == type.Value);
        }

        var devices = await query.ToListAsync(cancellationToken);

        if (isOn.HasValue)
        {
            return devices.Where(d => d.IsOn() == isOn.Value).ToList();
        }

        return devices;
    }

    /// <inheritdoc />
    public async Task<DomainDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Devices
            .FirstOrDefaultAsync(device => device.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DomainDevice?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Devices
            .AsNoTracking() // Read-only: single device
            .FirstOrDefaultAsync(device => device.Id == id, cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task AddAsync(DomainDevice device, CancellationToken cancellationToken = default)
    {
        // Stage new device for insertion
        // Persisted on SaveChangesAsync
        await dbContext.Devices.AddAsync(device, cancellationToken);
    }

    /// <summary>
    /// Deletes the device with the specified identifier.
    /// Uses <see cref="RelationalQueryableExtensions.ExecuteDeleteAsync"/> for immediate persistence.
    /// </summary>
    /// <param name="id">The unique identifier for the device to delete.</param>
    /// <returns>True when a matching device was found and deleted; otherwise false.</returns>
    public async Task<bool> RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        int deletedCount = await dbContext.Devices
            .Where(device => device.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedCount > 0;
    }

    /// <inheritdoc />
    public async Task<bool> ThermostatExistsAtLocationAsync(string location, CancellationToken cancellationToken = default)
    {
        return await dbContext.Thermostats
            // Enforce invariant: only one thermostat per location
            .AnyAsync(thermostat => thermostat.Location == location, cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<ThermostatDevice>> GetThermostatsByLocationAsync(
        string location, CancellationToken cancellationToken = default)
    {
        return await dbContext.Thermostats
            .Where(t => t.Location == location)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Commit all pending changes to the database
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<ITickable>> GetTickableAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Devices
            .OfType<TickableDevice>()
            .ToListAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task ResetAllAsync(CancellationToken cancellationToken = default)
    {
        var devices = await dbContext.Devices.ToListAsync(cancellationToken);

        foreach (var device in devices)
            device.ResetToDefaults();
    }

    /// <inheritdoc />
    public async Task LogActionAsync(Guid deviceId, string operation, CancellationToken cancellationToken = default)
    {
        var entry = new CommandHistory(deviceId, operation);
        await dbContext.DeviceHistory.AddAsync(entry, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommandHistory>> GetHistoryAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        return await dbContext.DeviceHistory
            .AsNoTracking()
            .Where(h => h.DeviceId == deviceId)
            .OrderByDescending(h => h.Timestamp)
            .ToListAsync(cancellationToken);
    }
}