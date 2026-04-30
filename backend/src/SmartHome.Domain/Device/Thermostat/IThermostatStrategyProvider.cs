using SmartHome.Domain.Common.Exceptions;

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
    /// <exception cref="InvalidDomainOperationException">
    /// Thrown if no strategy is registered for the specified mode.
    /// </exception>
    IThermostatModeStrategy GetStrategy(ThermostatMode mode);
}
