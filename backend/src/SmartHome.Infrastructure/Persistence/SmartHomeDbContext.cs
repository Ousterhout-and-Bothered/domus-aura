using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Device;

namespace SmartHome.Infrastructure.Persistence;

public sealed class SmartHomeDbContext : DbContext
{
    public SmartHomeDbContext(DbContextOptions<SmartHomeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Light> Lights => Set<Light>();
    public DbSet<Fan> Fans => Set<Fan>();
    public DbSet<Thermostat> Thermostats => Set<Thermostat>();
    public DbSet<DoorLock> DoorLocks => Set<DoorLock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>()
            .HasDiscriminator(device => device.Type)
            .HasValue<Light>(DeviceType.Light)
            .HasValue<Fan>(DeviceType.Fan)
            .HasValue<Thermostat>(DeviceType.Thermostat)
            .HasValue<DoorLock>(DeviceType.DoorLock);

        base.OnModelCreating(modelBuilder);
    }
}