using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Device.DoorLock;
using SmartHome.Domain.Device.Fan;
using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Device.Thermostat;
using DomainDevice = SmartHome.Domain.Device.Device;

namespace SmartHome.Infrastructure.Persistence.Seed;

public sealed class SmartHomeDbSeeder
{
    private readonly SmartHomeDbContext _dbContext;

    public SmartHomeDbSeeder(SmartHomeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Devices.AnyAsync(cancellationToken))
            return;

        var devices = new DomainDevice[]
        {
            new DoorLock("Front Door", "Entryway"),
            new DoorLock("Back Door", "Patio"),
            new Fan("Living Room Fan", "Living Room"),
            new Fan("Bedroom Fan", "Bedroom"),
            new Light("Kitchen Overhead", "Kitchen"),
            new Light("Living Room Overhead", "Living Room"),
            new Thermostat("Living Room Thermostat", "Living Room")
        };

        await _dbContext.Devices.AddRangeAsync(devices, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}