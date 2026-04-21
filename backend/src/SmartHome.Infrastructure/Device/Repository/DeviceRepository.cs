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
    
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => dbContext.Devices.AnyAsync(cancellationToken);

    
    public async Task<IReadOnlyList<DomainDevice>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Devices
            .AsNoTracking() // Read-only query 
            .ToListAsync(cancellationToken);
    }

    public async Task<DomainDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Devices
            .AsNoTracking() // Read-only lookup
            .FirstOrDefaultAsync(device => device.Id == id, cancellationToken);
    }

    public async Task AddAsync(DomainDevice device, CancellationToken cancellationToken = default)
    {
        // Stage new device for insertion
        // Persisted on SaveChangesAsync
        await dbContext.Devices.AddAsync(device, cancellationToken);
    }

    public async Task<bool> RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Lookup device to remove
        DomainDevice? device = await dbContext.Devices
            .FirstOrDefaultAsync(device => device.Id == id, cancellationToken);

        // Return false if device does not exist
        if (device is null)
            return false;

        // Mark entity for deletion
        // Executed on SaveChangesAsync
        dbContext.Devices.Remove(device);
        return true;
    }

    public async Task<bool> ThermostatExistsAtLocationAsync(string location, CancellationToken cancellationToken = default)
    {
        return await dbContext.Thermostats
            // Enforce invariant: only one thermostat per location
            .AnyAsync(thermostat => thermostat.Location == location, cancellationToken);
    }
    
    public async Task<IReadOnlyList<ThermostatDevice>> GetThermostatsByLocationAsync(
        string location, CancellationToken cancellationToken = default)
    {
        // Change-tracked query — ambient-temperature mutations must be persisted.
        return await dbContext.Thermostats
            .Where(t => t.Location == location)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Commit all pending changes to the database
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<ITickable>> GetTickableAsync(CancellationToken cancellationToken = default)
    {
        // Change-tracked query so Tick() mutations persist on SaveChangesAsync.
        var thermostats = await dbContext.Thermostats.ToListAsync(cancellationToken);
        return thermostats;
    }
    
    public async Task ResetAllAsync(CancellationToken cancellationToken = default)
    {
        // Single tracked load + mutate + save so every device resets in one unit of work.
        var devices = await dbContext.Devices.ToListAsync(cancellationToken);

        foreach (var device in devices)
            device.ResetToDefaults();

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}