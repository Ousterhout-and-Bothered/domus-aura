using SmartHome.Domain.Enum;

namespace SmartHome.Domain.Device.Interface;


/// <summary>
/// Defines the control contract for thermostat devices.
/// Thermostats are not simply "powered" — they transition through
/// operational states (Off, Idle, Heating, Cooling) rather than
/// a binary power state.
/// </summary>
public interface IThermostatControllable
{
    
    /// <summary>
    /// The current operational state of the thermostat.
    /// </summary>
    ThermostatState State { get; }

    /// <summary>
    /// Powers the thermostat on, transitioning to Idle and immediately
    /// evaluating whether heating or cooling should begin.
    /// </summary>
    void TurnOn();
    
    /// <summary>
    /// Powers the thermostat off from any active state.
    /// </summary>
    void TurnOff();
    
    /// <summary>
    /// Sets the operating mode (Heat, Cool, Auto).
    /// </summary>
    void SetMode(ThermostatMode mode);
    
    /// <summary>
    /// Sets the desired temperature in Fahrenheit.
    /// </summary>
    void SetDesiredTemperature(int temperature);
    
    /// <summary>
    /// Updates the ambient temperature of the location.
    /// </summary>
    void SetAmbientTemperature(int temperature);
}