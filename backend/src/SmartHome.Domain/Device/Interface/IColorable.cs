namespace SmartHome.Domain.Device.Interface;


/// <summary>
/// Defines the contract for devices that support RGB color control.
/// Color may only be changed while the device is powered on.
/// </summary>
public interface IColorable
{
    /// <summary>
    /// The current color as a hex string
    /// </summary>
    string ColorHex { get; }
    
    
    /// <summary>
    /// Sets the color of the device using a hex color string.
    /// Throws <see cref="InvalidOperationException"/> if the device is off.
    /// Throws <see cref="ArgumentException"/> if the hex format is invalid.
    /// </summary>
    void SetColor(string colorHex);

}