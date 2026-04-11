using SmartHome.Domain.Enum;

namespace SmartHome.Domain.Device;

public sealed class Light : PoweredDevice
{
    public int Brightness { get; private set; }
    public string ColorHex { get; private set; }

    // Required for EF Core
    private Light()
    {
        Type = DeviceType.Light;
        Brightness = 100;
        ColorHex = "#FFFFFF";
    }

    public Light(string name, string location)
        : base(name, location, DeviceType.Light)
    {
        Brightness = 100;
        ColorHex = "#FFFFFF";
    }

    public void SetBrightness(int brightness)
    {
        if (PowerState != PowerState.On)
            throw new InvalidOperationException("Brightness can only be changed while the light is on.");

        Brightness = Math.Clamp(brightness, 10, 100);
    }

    public void SetColor(string colorHex)
    {
        if (PowerState != PowerState.On)
            throw new InvalidOperationException("Color can only be changed while the light is on.");

        if (string.IsNullOrWhiteSpace(colorHex))
            throw new ArgumentException("Color is required.", nameof(colorHex));

        ColorHex = colorHex.Trim().ToUpperInvariant();
    }
}