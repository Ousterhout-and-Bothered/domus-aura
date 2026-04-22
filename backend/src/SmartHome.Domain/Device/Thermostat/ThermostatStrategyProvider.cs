using System.Collections.Concurrent;

namespace SmartHome.Domain.Device.Thermostat;

/// <summary>
/// Simple implementation of the strategy provider that manages
/// the mapping between modes and their respective logic.
/// </summary>
public sealed class ThermostatStrategyProvider : IThermostatStrategyProvider
{
    private static readonly ConcurrentDictionary<ThermostatMode, IThermostatModeStrategy> Strategies = new();

    static ThermostatStrategyProvider()
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
    public static void Register(ThermostatMode mode, IThermostatModeStrategy strategy)
    {
        Strategies[mode] = strategy;
    }

    public IThermostatModeStrategy GetStrategy(ThermostatMode mode)
    {
        if (Strategies.TryGetValue(mode, out var strategy))
            return strategy;

        throw new NotSupportedException($"No strategy registered for thermostat mode: {mode}");
    }
}
