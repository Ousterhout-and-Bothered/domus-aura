using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Device;
using SmartHome.Domain.Enum;

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

        var devices = new Device[]
        {
            new Light("Living Room Overhead", "Living Room"),
            new Light("Kitchen Pendant", "Kitchen"),
            new Fan("Living Room Fan", "Living Room"),
            new Fan("Bedroom Fan", "Bedroom"),
            new DoorLock("Front Door", "Entryway"),
            new DoorLock("Back Door", "Patio"),
            new Thermostat("Living Room Thermostat", "Living Room")
        };

        await _dbContext.Devices.AddRangeAsync(devices, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}