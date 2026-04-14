namespace SmartHome.Domain.Device.Thermostat;


/// <summary>
/// Cooling mode — system may only cool.
/// Transitions to Cooling when ambient is above desired.
/// Will never heat even if ambient is lower desired.
/// </summary>
public sealed class CoolModeStrategy : IThermostatModeStrategy
{
    public ThermostatState Evaluate(int ambient, int desired) =>
        ambient > desired
            ? ThermostatState.Cooling
            : ThermostatState.Idle;
}