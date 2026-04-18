using Microsoft.EntityFrameworkCore;
using DomainDevice = SmartHome.Domain.Device.Device;
using SmartHome.Domain.Device.Repository;
using SmartHome.Infrastructure.Persistence;

namespace SmartHome.Infrastructure.Device.Repository;

/// <summary>
/// EF Core implementation of the device repository contract.
/// Responsible for persistence concerns for devices.
/// </summary>
public sealed class DeviceRepository : IDeviceRepository
{
    private readonly SmartHomeDbContext _dbContext;

    public DeviceRepository(SmartHomeDbContext dbContext)
    {
        // Inject DbContext
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DomainDevice>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Devices
            .AsNoTracking() // Read-only query 
            .ToListAsync(cancellationToken);
    }

    public async Task<DomainDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Devices
            .AsNoTracking() // Read-only lookup
            .FirstOrDefaultAsync(device => device.Id == id, cancellationToken);
    }

    public async Task AddAsync(DomainDevice device, CancellationToken cancellationToken = default)
    {
        // Stage new device for insertion
        // Persisted on SaveChangesAsync
        await _dbContext.Devices.AddAsync(device, cancellationToken);
    }

    public async Task<bool> RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Lookup device to remove
        DomainDevice? device = await _dbContext.Devices
            .FirstOrDefaultAsync(device => device.Id == id, cancellationToken);

        // Return false if device does not exist
        if (device is null)
            return false;

        // Mark entity for deletion
        // Executed on SaveChangesAsync
        _dbContext.Devices.Remove(device);
        return true;
    }

    public async Task<bool> ThermostatExistsAtLocationAsync(string location, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Thermostats
            // Enforce invariant: only one thermostat per location
            .AnyAsync(thermostat => thermostat.Location == location, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Commit all pending changes to the database
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}