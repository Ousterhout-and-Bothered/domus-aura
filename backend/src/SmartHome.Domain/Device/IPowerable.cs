namespace SmartHome.Domain.Device;


/// <summary>
/// Powered devices can be turned on and off, and their functional
/// attributes (brightness, speed, etc.) are only meaningful when on.
/// </summary>
public interface IPowerable
{
    
    /// <summary>
    /// The current power state of the device.
    /// </summary>
    PowerState PowerState { get; }

    
    /// <summary>
    /// Powers the device on and activates its last known substate.
    /// </summary>
    void TurnOn();
    
    
    /// <summary>
    /// Powers the device off. Substate is retained for when it powers back on.
    /// </summary>
    void TurnOff();
    
}