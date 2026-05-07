using SmartHome.Domain.Common;
using SmartHome.Domain.Common.Exceptions;
using System.Text.RegularExpressions;

namespace SmartHome.Domain.Device.Light;

/// <summary>
/// Represents a smart light device that supports power, brightness, and color control.
/// Brightness and color are retained when the light is powered off and restored when powered back on.
/// </summary>
/// <remarks>
/// As of the implicit-power-on refactor, calling <see cref="SetBrightness"/> or
/// <see cref="SetColor"/> on a powered-off light no longer throws. Callers (the
/// command layer) check the power state and invoke <see cref="PoweredDevice.TurnOn"/>
/// before mutating brightness/color. Direct calls to these methods on an off light
/// will silently set the value but the change won't be visible until the light is
/// powered on — this is acceptable because the only legitimate caller is the command
/// layer, which always satisfies the precondition.
/// </remarks>
public sealed class Light : PoweredDevice, IDimmable, IColorable
{
    /// <summary>
    /// Gets the current brightness level as an integer percentage (10–100).
    /// </summary>
    public int Brightness { get; private set; }

    /// <summary>
    /// Gets the current color as a hex string (e.g. "#FF8800").
    /// </summary>
    public string ColorHex { get; private set; }

    // Required for EF Core
    private Light()
    {
        Type = DeviceType.Light;
        Brightness = 100;
        ColorHex = "#FFFFFF";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Light"/> class with a specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier for the light.</param>
    /// <param name="name">The user-facing name of the light.</param>
    /// <param name="location">The location of the light.</param>
    public Light(Guid id, string name, string location)
        : base(id, name, location, DeviceType.Light)
    {
        Brightness = 100;
        ColorHex = "#FFFFFF";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Light"/> class with a generated identifier.
    /// </summary>
    /// <param name="name">The user-facing name of the light.</param>
    /// <param name="location">The location of the light.</param>
    public Light(string name, string location)
        : base(name, location, DeviceType.Light)
    {
        Brightness = 100;
        ColorHex = "#FFFFFF";
    }

    /// <inheritdoc />
    /// <param name="brightness">The desired brightness value, clamped to [10, 100].</param>
    public void SetBrightness(int brightness)
    {
        // No power-state guard — command layer ensures precondition.
        Brightness = Guard.Clamp(brightness, 10, 100);
    }

    /// <summary>
    /// Sets the color of the light using a hex color string.
    /// Throws <see cref="InvalidDomainArgumentException"/> if the hex format is invalid.
    /// </summary>
    /// <param name="colorHex">The hex color value to apply (e.g. "#FF8800").</param>
    public void SetColor(string colorHex)
    {
        // No power-state guard — command layer ensures precondition.
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
        // Reset brightness to the default (100%)
        Brightness = 100;
        // Reset color to default (white (#FFFFFF))
        ColorHex = "#FFFFFF";
    }

    /// <summary>
    /// Log-friendly representation including power and light-specific attributes.
    /// </summary>
    /// <returns>A string representation of the light including its current state.</returns>
    public override string ToString() =>
        $"{GetType().Name}(Id={Id}, Name='{Name}', Location='{Location}', " +
        $"Power={PowerState}, Brightness={Brightness}%, Color={ColorHex})";
}