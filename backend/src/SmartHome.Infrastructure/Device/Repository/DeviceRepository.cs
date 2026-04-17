using Microsoft.EntityFrameworkCore;
using DomainDevice = SmartHome.Domain.Device.Device;
using SmartHome.Domain.Device.Repository;
using SmartHome.Infrastructure.Persistence;

namespace SmartHome.Infrastructure.Device.Repository;

/// <summary>
/// EF Core implementation of the device repository contract.
/// Responsible only for persistence concerns for devices.
/// </summary>
public sealed class DeviceRepository : IDeviceRepository
{
    private readonly SmartHomeDbContext _dbContext;

    public DeviceRepository(SmartHomeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DomainDevice>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Devices
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<DomainDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Devices
            .AsNoTracking()
            .FirstOrDefaultAsync(device => device.Id == id, cancellationToken);
    }

    public async Task AddAsync(DomainDevice device, CancellationToken cancellationToken = default)
    {
        await _dbContext.Devices.AddAsync(device, cancellationToken);
    }

    public async Task<bool> RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        DomainDevice? device = await _dbContext.Devices
            .FirstOrDefaultAsync(device => device.Id == id, cancellationToken);

        if (device is null)
            return false;

        _dbContext.Devices.Remove(device);
        return true;
    }

    public async Task<bool> ThermostatExistsAtLocationAsync(string location, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Thermostats
            .AnyAsync(thermostat => thermostat.Location == location, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}