using SmartHome.Domain.Enum;

namespace SmartHome.Domain.Device;

public sealed class Thermostat : Device
{
    public ThermostatState State { get; private set; }
    public ThermostatMode Mode { get; private set; }
    public int DesiredTemperature { get; private set; }
    public int AmbientTemperature { get; private set; }
    
    private const int DefaultTemperature = 72;
    private const int MinTemperature = 60;
    private const int MaxTemperature = 80;

    // Required for EF Core
    private Thermostat()
    {
        Type = DeviceType.Thermostat;
        State = ThermostatState.Off;
        Mode = ThermostatMode.Auto;
        DesiredTemperature = DefaultTemperature;
        AmbientTemperature = DefaultTemperature;
    }

    public Thermostat(string name, string location)
        : base(name, location, DeviceType.Thermostat)
    {
        State = ThermostatState.Off;
        Mode = ThermostatMode.Auto;
        DesiredTemperature = DefaultTemperature;
        AmbientTemperature = DefaultTemperature;
    }

    public void TurnOn()
    {
        if (State == ThermostatState.Off)
        {
            State = ThermostatState.Idle;
            EvaluateState();
        }
    }

    public void TurnOff()
    {
        if (State != ThermostatState.Off)
            State = ThermostatState.Off;
    }

    public void SetMode(ThermostatMode mode)
    {
        Mode = mode;

        if (State != ThermostatState.Off)
            EvaluateState();
    }

    public void SetDesiredTemperature(int temperature)
    {
        DesiredTemperature = Math.Clamp(temperature, MinTemperature, MaxTemperature);

        if (State != ThermostatState.Off)
            EvaluateState();
    }

    public void SetAmbientTemperature(int temperature)
    {
        AmbientTemperature = temperature;

        if (State != ThermostatState.Off)
            EvaluateState();
    }

    public void Tick()
    {
        if (State == ThermostatState.Off || State == ThermostatState.Idle)
            return;

        if (State == ThermostatState.Heating && AmbientTemperature < DesiredTemperature)
        {
            AmbientTemperature++;
        }
        else if (State == ThermostatState.Cooling && AmbientTemperature > DesiredTemperature)
        {
            AmbientTemperature--;
        }

        EvaluateState();
    }

    private void EvaluateState()
    {
        if (State == ThermostatState.Off)
            return;

        if (AmbientTemperature == DesiredTemperature)
        {
            State = ThermostatState.Idle;
            return;
        }

        switch (Mode)
        {
            case ThermostatMode.Heat:
                State = AmbientTemperature < DesiredTemperature
                    ? ThermostatState.Heating
                    : ThermostatState.Idle;
                break;

            case ThermostatMode.Cool:
                State = AmbientTemperature > DesiredTemperature
                    ? ThermostatState.Cooling
                    : ThermostatState.Idle;
                break;

            case ThermostatMode.Auto:
                if (AmbientTemperature < DesiredTemperature)
                    State = ThermostatState.Heating;
                else if (AmbientTemperature > DesiredTemperature)
                    State = ThermostatState.Cooling;
                else
                    State = ThermostatState.Idle;
                break;
        }
    }

    public override bool IsOn() =>
        State == ThermostatState.Heating || State == ThermostatState.Cooling;
}