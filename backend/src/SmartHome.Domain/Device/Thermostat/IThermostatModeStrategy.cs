namespace SmartHome.Domain.Device.Thermostat;


/// <summary>
/// Defines the contract for thermostat mode behavior.
/// Each mode determines when the thermostat should heat, cool, or idle
/// based on ambient vs desired temperature.
/// Implement this interface to add a new mode without modifying any existing code
/// (Open/Closed Principle).
/// </summary>
public interface IThermostatModeStrategy
{
    
    /// <summary>
    /// Evaluates ambient vs desired temperature and returns
    /// the appropriate thermostat state for this mode.
    /// </summary>
    ThermostatState Evaluate(int ambientTemperature, int desiredTemperature);
}