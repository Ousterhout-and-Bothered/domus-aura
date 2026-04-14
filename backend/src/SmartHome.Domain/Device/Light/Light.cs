using System.Text.RegularExpressions;

namespace SmartHome.Domain.Device.Light;


/// <summary>
/// Represents a smart light device that supports power, brightness, and color control.
/// Brightness and color are retained when the light is powered off and restored when powered back on.
/// </summary>
public sealed class Light : PoweredDevice, IDimmable, IColorable
{
    
    /// <summary>
    /// The current brightness level as an integer percentage (10–100).
    /// </summary>
    public int Brightness { get; private set; }
    
    /// <summary>
    /// The current color as a hex string (e.g. "#FF8800").
    /// </summary>
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

    
    /// <summary>
    /// Sets the brightness of the light.
    /// Throws <see cref="InvalidOperationException"/> if the light is off.
    /// Throws <see cref="ArgumentOutOfRangeException"/> if brightness is outside 10–100.
    /// </summary>
    public void SetBrightness(int brightness)
    {
        if (PowerState != PowerState.On)
            throw new InvalidOperationException("Brightness can only be changed while the light is on.");
        
        // Reject out-of-range values explicitly — domain should never silently correct bad input.
        // Never trust data from the browser!
        if (brightness < 10 || brightness > 100)
            throw new ArgumentOutOfRangeException(nameof(brightness), 
                "Brightness must be between 10 and 100.");
            
        Brightness = brightness;
    }

    
    /// <summary>
    /// Sets the color of the light using a hex color string.
    /// Throws <see cref="InvalidOperationException"/> if the light is off.
    /// Throws <see cref="ArgumentException"/> if the hex format is invalid.
    /// </summary>
    public void SetColor(string colorHex)
    {
        if (PowerState != PowerState.On)
            throw new InvalidOperationException("Color can only be changed while the light is on.");

        if (string.IsNullOrWhiteSpace(colorHex))
            throw new ArgumentException("Color is required.", nameof(colorHex));
        
        // Validate hex format — prevents garbage values
        if (!Regex.IsMatch(colorHex.Trim(), "^#[0-9a-fA-F]{6}$"))
            throw new ArgumentException("Color must be a valid hex color.", nameof(colorHex));

        ColorHex = colorHex.Trim().ToUpperInvariant();
    }
}