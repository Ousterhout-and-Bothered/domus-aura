using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.DoorLock;
using SmartHome.Domain.Device.Fan;
using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Device.Thermostat;
using DomainDevice = SmartHome.Domain.Device.Device;

namespace SmartHome.Infrastructure.Persistence;

public sealed class SmartHomeDbContext : DbContext
{
    public SmartHomeDbContext(DbContextOptions<SmartHomeDbContext> options) : base(options)
    {
        // Pass configured DbContext options to base constructor
    }

    // Base table for all devices (TPH inheritance root)
    public DbSet<DomainDevice> Devices => Set<DomainDevice>();

    // Typed access for querying specific device types
    public DbSet<Light> Lights => Set<Light>();
    public DbSet<Fan> Fans => Set<Fan>();
    public DbSet<Thermostat> Thermostats => Set<Thermostat>();
    public DbSet<DoorLock> DoorLocks => Set<DoorLock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DomainDevice>()
            // Configure TPH inheritance using discriminator column
            .HasDiscriminator(device => device.Type)

            // Map each concrete type to its corresponding discriminator value
            .HasValue<Light>(DeviceType.Light)
            .HasValue<Fan>(DeviceType.Fan)
            .HasValue<Thermostat>(DeviceType.Thermostat)
            .HasValue<DoorLock>(DeviceType.DoorLock);

        // Allow EF Core to apply any additional default configurations
        base.OnModelCreating(modelBuilder);
    }
}