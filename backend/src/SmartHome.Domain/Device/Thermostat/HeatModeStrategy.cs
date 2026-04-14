namespace SmartHome.Domain.Device.Thermostat;


/// <summary>
/// Heat mode — system may only heat.
/// Transitions to Heating when ambient is below desired.
/// Will never cool even if ambient exceeds desired.
/// </summary>
public sealed class HeatModeStrategy : IThermostatModeStrategy
{
    public ThermostatState Evaluate(int ambient, int desired) =>
        ambient < desired
            ? ThermostatState.Heating
            : ThermostatState.Idle;
}