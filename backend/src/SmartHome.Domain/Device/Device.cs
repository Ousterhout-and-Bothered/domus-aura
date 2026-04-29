using SmartHome.Domain.Common;
using System.Runtime.CompilerServices;

namespace SmartHome.Domain.Device;

/// <summary>
/// Abstract base class for all smart home devices.
/// Enforces that every device has a unique identity,
/// a human-readable name, a location, and a type.
/// </summary>
public abstract class Device : IDevice
{
    /// <summary>
    /// The unique identifier for this device.
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// The human-readable name of the device (e.g. "Living Room Overhead").
    /// </summary>
    public string Name { get; protected set; }

    /// <summary>
    /// The room or area the device is located in (e.g. "Kitchen").
    /// </summary>
    public string Location { get; protected set; }

    /// <summary>
    /// The type of device (Light, Fan, Thermostat, DoorLock).
    /// </summary>
    public DeviceType Type { get; protected set; }

    // Required for EF Core
    protected Device()
    {
        Name = string.Empty;
        Location = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Device"/> class with a predefined identifier.
    /// </summary>
    /// <param name="id">The unique identifier for the device.</param>
    /// <param name="name">The human-readable name of the device.</param>
    /// <param name="location">The location of the device.</param>
    /// <param name="type">The type of the device.</param>
    protected Device(Guid id, string name, string location, DeviceType type)
    {
        Id = id;

        Name = Guard.NotNullOrWhitespace(name, "Device name is required.");
        Location = Guard.NotNullOrWhitespace(location, "Device location is required.");

        Type = type;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Device"/> class with a generated identifier.
    /// </summary>
    /// <param name="name">The human-readable name of the device.</param>
    /// <param name="location">The location of the device.</param>
    /// <param name="type">The type of the device.</param>
    protected Device(string name, string location, DeviceType type)
    {
        Id = Guid.NewGuid();

        Name = Guard.NotNullOrWhitespace(name, "Device name is required.");
        Location = Guard.NotNullOrWhitespace(location, "Device location is required.");

        Type = type;
    }

    /// <summary>
    /// Renames the device.
    /// Throws <see cref="ArgumentException"/> if the name is null or whitespace.
    /// </summary>
    /// <param name="name">The new name for the device.</param>
    public void Rename(string name)
    {
        Name = Guard.NotNullOrWhitespace(name, "Device name is required.");
    }

    /// <summary>
    /// Moves the device to a new location.
    /// Throws <see cref="ArgumentException"/> if the location is null or whitespace.
    /// </summary>
    /// <param name="location">The new location for the device.</param>
    public void Relocate(string location)
    {
        Location = Guard.NotNullOrWhitespace(location, "Device location is required.");
    }

    /// <summary>
    /// Returns true when the device is in an active "on" state.
    /// Latch devices always return true.
    /// Powered devices return true only when powered on.
    /// </summary>
    public abstract bool IsOn();

    /// <summary>
    /// Entity equality: two devices are equal if and only if they share the same <see cref="Id"/>.
    /// Transient devices (Id = Guid.Empty, not yet persisted or fully rehydrated) compare by
    /// reference — two separately constructed transient instances are considered distinct entities
    /// that will receive distinct IDs once persistence completes.
    /// Mutable attributes (Name, Location, state) are intentionally excluded from equality.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns>True if the devices are equal; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Device other)
            return false;

        if (Id == Guid.Empty || other.Id == Guid.Empty)
            return ReferenceEquals(this, other);

        return Id.Equals(other.Id);
    }

    /// <summary>
    /// Hashed on the immutable <see cref="Id"/> once assigned.
    /// Transient devices fall back to reference-based hashing so separate unidentified
    /// instances do not collide into the same hash bucket.
    /// </summary>
    /// <returns>The hash code for this device.</returns>
    public override int GetHashCode()
    {
        var id = Id;

        return id == Guid.Empty
            ? RuntimeHelpers.GetHashCode(this)
            : id.GetHashCode();
    }

    /// <summary>
    /// Produces a log-friendly representation of the device.
    /// Uses <c>GetType().Name</c> so subclasses (Light, Fan, etc.) are identified correctly
    /// without each subclass having to override <see cref="ToString"/>.
    /// </summary>
    /// <returns>A string representation of the device.</returns>
    public override string ToString() =>
        $"{GetType().Name}(Id={Id}, Name='{Name}', Location='{Location}')";

    /// <summary>
    /// Resets this device to its default settings.
    /// </summary>
    public abstract void ResetToDefaults();
}