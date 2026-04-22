using SmartHome.Domain.Common;
using SmartHome.Domain.Common.Exceptions;
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


    /// <inheritdoc />
    public void SetBrightness(int brightness)
    {
        Guard.AgainstInvalidState(PowerState == PowerState.On, "Brightness can only be changed while the light is on.");

        // Clamped to [10, 100] as per README requirement.
        Brightness = Guard.Clamp(brightness, 10, 100);
    }


    /// <summary>
    /// Sets the color of the light using a hex color string.
    /// Throws <see cref="InvalidDomainOperationException"/> if the light is off.
    /// Throws <see cref="InvalidDomainArgumentException"/> if the hex format is invalid.
    /// </summary>
    public void SetColor(string colorHex)
    {
        Guard.AgainstInvalidState(PowerState == PowerState.On, "Color can only be changed while the light is on.");

        colorHex = Guard.NotNullOrWhitespace(colorHex, "Color is required.");

        // Validate hex format — prevents garbage values
        if (!Regex.IsMatch(colorHex, "^#[0-9a-fA-F]{6}$"))
            throw new InvalidDomainArgumentException("Color must be a valid hex color.");

        ColorHex = colorHex.ToUpperInvariant();
    }

    /// <summary>
    /// Resets powered device attributes for the light to their default values.
    /// </summary>
    protected override void ResetPoweredDefaults()
    {
        // Reset brightness to default (100%)
        Brightness = 100;
        // Reset color to default (white (#FFFFFF))
        ColorHex = "#FFFFFF";
    }

    /// <summary>
    /// Log-friendly representation including power and light-specific attributes.
    /// </summary>
    public override string ToString() =>
        $"{GetType().Name}(Id={Id}, Name='{Name}', Location='{Location}', " +
        $"Power={PowerState}, Brightness={Brightness}%, Color={ColorHex})";
}