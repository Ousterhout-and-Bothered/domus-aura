using SmartHome.Domain.Device.Thermostat;

namespace SmartHome.Domain.Device.Thermostat;

/// <summary>
/// Provides strategy instances for thermostat modes.
/// Implementation of the Strategy pattern to allow extending modes
/// without modifying the Thermostat class.
/// </summary>
public interface IThermostatStrategyProvider
{
    /// <summary>
    /// Returns the strategy implementation for the given thermostat mode.
    /// </summary>
    IThermostatModeStrategy GetStrategy(ThermostatMode mode);
}
