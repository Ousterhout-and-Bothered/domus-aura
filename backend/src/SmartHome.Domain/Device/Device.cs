using SmartHome.Domain.Common;
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

    protected Device(string name, string location, DeviceType type)
    {
        // Generate unique identifier for new device
        Id = Guid.NewGuid();

        // Validate and normalize required fields
        Name = Guard.NotNullOrWhitespace(name, "Device name is required.");
        Location = Guard.NotNullOrWhitespace(location, "Device location is required.");

        // Assign device type
        Type = type;
    }

    /// <summary>
    /// Renames the device.
    /// Throws <see cref="ArgumentException"/> if the name is null or whitespace.
    /// </summary>
    public void Rename(string name)
    {
        Name = Guard.NotNullOrWhitespace(name, "Device name is required.");
    }


    /// <summary>
    /// Moves the device to a new location.
    /// Throws <see cref="ArgumentException"/> if the location is null or whitespace.
    /// </summary>
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
    public override bool Equals(object? obj)
    {
        if (obj is not Device other) return false;

        // Transient (not-yet-identified) entities use reference equality.
        // Two separately constructed transient devices must NOT compare equal just because
        // both have Guid.Empty — they will become distinct devices once EF populates them.
        if (Id == Guid.Empty || other.Id == Guid.Empty)
            return ReferenceEquals(this, other);

        return Id.Equals(other.Id);
    }


    /// <summary>
    /// Hashed on the immutable <see cref="Id"/> once assigned.
    /// Transient devices fall back to reference-based hashing so separate unidentified
    /// instances do not collide into the same hash bucket.
    /// </summary>
    public override int GetHashCode() =>
        Id == Guid.Empty
            ? base.GetHashCode()        // object.GetHashCode() — reference-based
            : Id.GetHashCode();

    /// <summary>
    /// Produces a log-friendly representation of the device.
    /// Uses <c>GetType().Name</c> so subclasses (Light, Fan, etc.) are identified correctly
    /// without each subclass having to override <see cref="ToString"/>.
    /// </summary>
    public override string ToString() =>
        $"{GetType().Name}(Id={Id}, Name='{Name}', Location='{Location}')";

    /// <summary>
    /// Resets this device to its default settings.
    /// </summary>
    public abstract void ResetToDefaults();
}