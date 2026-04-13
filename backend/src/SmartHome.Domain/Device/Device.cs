using SmartHome.Domain.Enum;

namespace SmartHome.Domain.Device;

public abstract class Device
{
    public Guid Id { get; protected set; }
    public string Name { get; protected set; }
    public string Location { get; protected set; }
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

    public void Rename(string name)
    {
        Name = ValidateRequired(name, nameof(name), "Device name is required.");
    }

    public void Relocate(string location)
    {
        Location = ValidateRequired(location, nameof(location), "Device location is required.");
    }

    public abstract bool IsOn();
}