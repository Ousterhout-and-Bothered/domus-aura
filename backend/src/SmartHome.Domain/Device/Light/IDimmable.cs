namespace SmartHome.Domain.Device.Light;


/// <summary>
/// Defines the contract for devices that support variable brightness.
/// Brightness may only be changed while the device is powered on.
/// </summary>
public interface IDimmable
{

    /// <summary>
    /// The current brightness level as an integer percentage.
    /// Valid range is 10 to 100 inclusive.
    /// </summary>
    int Brightness { get; }
    
    
    /// <summary>
    /// Sets the brightness of the device.
    /// Throws <see cref="InvalidOperationException"/> if the device is off.
    /// Throws <see cref="ArgumentOutOfRangeException"/> if brightness is outside 10–100.
    /// </summary>
    void SetBrightness(int brightness);

}