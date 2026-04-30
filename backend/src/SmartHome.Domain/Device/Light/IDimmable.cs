using SmartHome.Domain.Common.Exceptions;

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
    /// Values outside the 10–100 range are clamped to the nearest bound.
    /// </summary>
    /// <exception cref="InvalidDomainOperationException">
    /// Thrown if the device is off.
    /// </exception>
    void SetBrightness(int brightness);

}