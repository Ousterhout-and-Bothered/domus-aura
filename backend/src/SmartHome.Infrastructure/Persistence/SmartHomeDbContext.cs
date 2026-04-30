using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.DoorLock;
using SmartHome.Domain.Device.Fan;
using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Device.Thermostat;
using SmartHome.Domain.Scene;
using DomainDevice = SmartHome.Domain.Device.Device;

namespace SmartHome.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for the Smart Home simulation.
/// Configures persistence for devices, command history, and inheritance mappings.
/// </summary>
public sealed class SmartHomeDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SmartHomeDbContext"/> class.
    /// </summary>
    /// <param name="options">The context configuration options.</param>
    public SmartHomeDbContext(DbContextOptions<SmartHomeDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets the base set of all devices in the system.
    /// Supports TPH (Table-Per-Hierarchy) inheritance.
    /// </summary>
    public DbSet<DomainDevice> Devices => Set<DomainDevice>();

    /// <summary>
    /// Gets the audit trail of all commands executed against devices.
    /// </summary>
    public DbSet<CommandHistory> DeviceHistory => Set<CommandHistory>();

    /// <summary>
    /// Gets the set of all light devices.
    /// </summary>
    public DbSet<Light> Lights => Set<Light>();

    /// <summary>
    /// Gets the set of all fan devices.
    /// </summary>
    public DbSet<Fan> Fans => Set<Fan>();

    /// <summary>
    /// Gets the set of all thermostat devices.
    /// </summary>
    public DbSet<Thermostat> Thermostats => Set<Thermostat>();

    /// <summary>
    /// Gets the set of all door lock devices.
    /// </summary>
    public DbSet<DoorLock> DoorLocks => Set<DoorLock>();

    /// <summary>
    /// Gets the set of all scenes.
    /// </summary>
    public DbSet<DeviceScene> Scenes => Set<DeviceScene>();

    /// <summary>
    /// Configures the database model, including inheritance discriminators and indexes.
    /// </summary>
    /// <param name="modelBuilder">The builder used to configure the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommandHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Index DeviceId for faster history lookups
            entity.HasIndex(e => e.DeviceId);
        });

        modelBuilder.Entity<DomainDevice>(entity =>
        {
            entity.ToTable("Devices")
                // Configure TPH inheritance using discriminator column
                .HasDiscriminator(d => d.Type)
                // Map each concrete type to its corresponding discriminator value
                .HasValue<Light>(DeviceType.Light)
                .HasValue<Fan>(DeviceType.Fan)
                .HasValue<Thermostat>(DeviceType.Thermostat)
                .HasValue<DoorLock>(DeviceType.DoorLock);

            // Enforce one-thermostat-per-location at the database level.
            // Partial unique index — applies only to rows where Type = Thermostat.
            // The in-memory pre-check in DeviceService catches the common case;
            // this is the transactional backstop for concurrent registrations.
            entity.HasIndex(d => d.Location)
                .IsUnique()
                .HasFilter($"\"Type\" = {(int)DeviceType.Thermostat}");

            // Ensure GUIDs are handled consistently in SQLite.
            // Using a string conversion and NOCASE collation avoids issues with case-sensitivity.
            entity.Property(d => d.Id)
                .HasConversion<string>()
                .HasColumnType("TEXT COLLATE NOCASE");
        });

        modelBuilder.Entity<DeviceScene>(entity =>
        {
            entity.ToTable("Scenes");
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Id)
                .HasConversion<string>()
                .HasColumnType("TEXT COLLATE NOCASE");

            entity.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasMany(s => s.Actions)
                .WithOne()
                .HasForeignKey("SceneId")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.Metadata.FindNavigation(nameof(DeviceScene.Actions))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<SceneAction>(entity =>
        {
            entity.ToTable("SceneActions");
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Id)
                .HasConversion<string>()
                .HasColumnType("TEXT COLLATE NOCASE");

            entity.Property(a => a.DeviceId)
                .HasConversion<string?>()
                .HasColumnType("TEXT COLLATE NOCASE");

            entity.Property(a => a.DeviceType)
                .HasConversion<string?>();

            entity.Property(a => a.Location)
                .HasMaxLength(100);

            entity.Property(a => a.Operation)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(a => a.Value)
                .HasMaxLength(50);

            entity.Property(a => a.OrderIndex)
                .IsRequired();

            entity.HasIndex("SceneId", nameof(SceneAction.OrderIndex));
        });

        // Explicitly include abstract intermediate classes in the model
        // so that OfType<TickableDevice>() can be translated by LINQ.
        modelBuilder.Entity<PoweredDevice>();
        modelBuilder.Entity<TickableDevice>();

        // Allow EF Core to apply any additional default configurations
        base.OnModelCreating(modelBuilder);
    }


}