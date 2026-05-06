using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Device;
using DomainDevice = SmartHome.Domain.Device.Device;
using SmartHome.Domain.Device.Repository;
using SmartHome.Infrastructure.Persistence;
using ThermostatDevice = SmartHome.Domain.Device.Thermostat.Thermostat;
using SmartHome.Domain.Common;

namespace SmartHome.Infrastructure.Device.Repository;

/// <summary>
/// EF Core implementation of the device repository contract.
/// Responsible for persistence concerns for devices.
/// </summary>
public sealed class DeviceRepository(SmartHomeDbContext dbContext) : EfRepositoryBase(dbContext), IDeviceRepository
{

    /// <inheritdoc />
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => dbContext.Devices.AnyAsync(cancellationToken);

    /// <inheritdoc />
    // In-memory filtering is acceptable here due to projects
    // status as a simulation. It prioritizes maintainability over
    // extreme scalability.
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
    /// </summary>
    /// <param name="id">The unique identifier for the device to delete.</param>
    /// <returns>True when a matching device was found and deleted; otherwise false.</returns>
    public async Task<bool> RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var affectedRows = await dbContext.Devices
            .Where(d => d.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return affectedRows > 0;
    }

    /// <inheritdoc />
    public async Task<bool> ThermostatExistsAtLocationAsync(string location,
        CancellationToken cancellationToken = default)
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
    public async Task<IReadOnlyList<CommandHistory>> GetHistoryAsync(Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DeviceHistory
            .AsNoTracking()
            .Where(h => h.DeviceId == deviceId)
            .OrderByDescending(h => h.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogActionAsync(Guid deviceId, string operation, CancellationToken cancellationToken = default)
    {
        var entry = new CommandHistory(deviceId, operation);
        await dbContext.DeviceHistory.AddAsync(entry, cancellationToken);
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
        // Base query: history rows joined with devices for the location filter.
        // Joined upfront so 'location' filters server-side rather than fetching unfiltered
        // history and pruning in memory.
        var query =
            from history in dbContext.DeviceHistory.AsNoTracking()
            join device in dbContext.Devices.AsNoTracking()
                on history.DeviceId equals device.Id
            select new { history, device };

        if (!string.IsNullOrWhiteSpace(location))
        {
            query = query.Where(x => x.device.Location == location);
        }

        if (deviceId.HasValue)
        {
            query = query.Where(x => x.history.DeviceId == deviceId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(x => x.history.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.history.Timestamp <= to.Value);
        }

        // CountAsync runs as a separate SELECT COUNT(*) against the same filter set.
        // Two round trips total; for SQLite at this scale, well under any latency budget.
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.history.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.history)
            .ToListAsync(cancellationToken);

        return new PagedResult<CommandHistory>(items, total, page, pageSize);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DomainDevice>> GetAllTrackedAsync(
        string? location = null,
        DeviceType? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Devices.AsQueryable();  // tracked by default

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(d => d.Location == location);

        if (type.HasValue)
            query = query.Where(d => d.Type == type.Value);

        return await query.ToListAsync(cancellationToken);
    }
}


