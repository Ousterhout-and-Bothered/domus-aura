namespace SmartHome.Domain.Device.Thermostat;


/// <summary>
/// Auto mode — system heats or cools automatically as needed.
/// Heats when ambient is below desired, cools when above.
/// </summary>
public sealed class AutoModeStrategy : IThermostatModeStrategy
{
    public ThermostatState Evaluate(int ambient, int desired)
    {
        if (ambient < desired) return ThermostatState.Heating;
        if (ambient > desired) return ThermostatState.Cooling;
        return ThermostatState.Idle;
    }

}