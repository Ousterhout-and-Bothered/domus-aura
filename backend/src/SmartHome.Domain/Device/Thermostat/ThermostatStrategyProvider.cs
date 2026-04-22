using System.Collections.Concurrent;
using SmartHome.Domain.Common.Exceptions;

namespace SmartHome.Domain.Device.Thermostat;

/// <summary>
/// Simple implementation of the strategy provider that manages
/// the mapping between modes and their respective logic.
/// </summary>
public sealed class ThermostatStrategyProvider : IThermostatStrategyProvider
{
    private readonly ConcurrentDictionary<ThermostatMode, IThermostatModeStrategy> _strategies = new();

    public ThermostatStrategyProvider()
    {
        // Register default strategies
        Register(ThermostatMode.Heat, new HeatModeStrategy());
        Register(ThermostatMode.Cool, new CoolModeStrategy());
        Register(ThermostatMode.Auto, new AutoModeStrategy());
    }

    /// <summary>
    /// Programmatically adds a new mode strategy.
    /// This allows adding new modes without changing the provider itself.
    /// </summary>
    public void Register(ThermostatMode mode, IThermostatModeStrategy strategy)
    {
        _strategies[mode] = strategy;
    }

    public IThermostatModeStrategy GetStrategy(ThermostatMode mode)
    {
        if (_strategies.TryGetValue(mode, out var strategy))
            return strategy;

        throw new InvalidDomainOperationException($"No strategy registered for thermostat mode: {mode}");
    }
}
