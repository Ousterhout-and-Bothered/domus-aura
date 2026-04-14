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
        Id = Guid.NewGuid();
        Name = ValidateRequired(name, nameof(name), "Device name is required.");
        Location = ValidateRequired(location, nameof(location), "Device location is required.");
        Type = type;
    }
    
    private static string ValidateRequired(string value, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(message, paramName);

        return value.Trim();
    }

    
    /// <summary>
    /// Renames the device.
    /// Throws <see cref="ArgumentException"/> if the name is null or whitespace.
    /// </summary>
    public void Rename(string name)
    {
        Name = ValidateRequired(name, nameof(name), "Device name is required.");
    }

    
    /// <summary>
    /// Moves the device to a new location.
    /// Throws <see cref="ArgumentException"/> if the location is null or whitespace.
    /// </summary>
    public void Relocate(string location)
    {
        Location = ValidateRequired(location, nameof(location), "Device location is required.");
    }

    
    /// <summary>
    /// Returns true when the device is in an active "on" state.
    /// Latch devices always return true.
    /// Powered devices return true only when powered on.
    /// </summary>
    public abstract bool IsOn();
}