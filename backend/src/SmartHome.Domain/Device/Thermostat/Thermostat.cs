namespace SmartHome.Domain.Device.Thermostat;


/// <summary>
/// Represents a smart thermostat that monitors and controls the ambient temperature
/// of a location. Supports heating, cooling, and auto modes.
/// Transitions between Off, Idle, Heating, and Cooling states based on the
/// relationship between ambient and desired temperature.
/// </summary>
public sealed class Thermostat : Device, IThermostatControllable
{
    
    /// <summary>
    /// The current state of the thermostat.
    /// </summary>
    public ThermostatState State { get; private set; }
    
    private ThermostatMode _mode;

    /// <summary>
    /// The current mode controlling heating/cooling behavior.
    /// When set, automatically updates the active strategy so EF Core
    /// rehydration always produces a consistent state.
    /// </summary>
    public ThermostatMode Mode
    {
        get => _mode;
        private set
        {
            _mode = value;
            _strategy = Strategies[value];
        }
    }
    
    /// <summary>
    /// The target temperature in Fahrenheit. Clamped to 60–80°F.
    /// </summary>
    public int DesiredTemperature { get; private set; }
    
    /// <summary>
    /// The current ambient temperature of the location in Fahrenheit.
    /// </summary>
    public int AmbientTemperature { get; private set; }
    
    private const int DefaultTemperature = 72;
    private const int MinTemperature = 60;
    private const int MaxTemperature = 80;

    private IThermostatModeStrategy _strategy = null!;
    
    // Strategy map — adding a new mode only requires a new class and entry here.
    // Thermostat.cs never changes — Open/Closed Principle.
    private static readonly Dictionary<ThermostatMode, IThermostatModeStrategy>
            Strategies = new()
            {
                { ThermostatMode.Heat, new HeatModeStrategy() },
                { ThermostatMode.Cool, new CoolModeStrategy() },
                { ThermostatMode.Auto, new AutoModeStrategy() }
            };
    
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
    
    /// <summary>
    /// Powers the thermostat on, transitioning to Idle and immediately
    /// evaluating whether heating or cooling should begin.
    /// </summary>
    public void TurnOn()
    {
        if (State != ThermostatState.Off)
            throw new InvalidOperationException("Thermostat is already on.");
        State = ThermostatState.Idle;
        EvaluateState();
    }

    
    /// <summary>
    /// Powers the thermostat off from any active state.
    /// </summary>
    public void TurnOff()
    {
        if (State == ThermostatState.Off)
           throw new InvalidOperationException("Thermostat is already off.");
        State = ThermostatState.Off;
    }

    
    /// <summary>
    /// Sets the operating mode and immediately re-evaluates state.
    /// </summary>
    public void SetMode(ThermostatMode mode)
    {
        Mode = mode;
        
        if (State != ThermostatState.Off)
            EvaluateState();
    }

    
    /// <summary>
    /// Sets the desired temperature in Fahrenheit.
    /// Clamp rather than reject. Thermostat silently enforces its own operating limits,
    /// consistent with how physical thermostats behave at their min/max range.
    /// </summary>
    public void SetDesiredTemperature(int temperature)
    {
        DesiredTemperature = Math.Clamp(temperature, MinTemperature, MaxTemperature);

        if (State != ThermostatState.Off)
            EvaluateState();
    }

    
    /// <summary>
    /// Updates the ambient temperature of the location and re-evaluates state.
    /// Changes are tracked even when the thermostat is off.
    /// </summary>
    public void SetAmbientTemperature(int temperature)
    {
        AmbientTemperature = temperature;

        if (State != ThermostatState.Off)
            EvaluateState();
    }

    
    /// <summary>
    /// Advances the simulation by one tick. Adjusts ambient temperature
    /// by 1°F toward the desired temperature and re-evaluates state.
    /// Has no effect when Off or Idle.
    /// </summary>
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

    
    /// <summary>
    /// Delegates evaluation to the active mode strategy.
    /// </summary>
    private void EvaluateState()
    {
        if (State == ThermostatState.Off) return;
        State = _strategy.Evaluate(AmbientTemperature, DesiredTemperature);
    }

    
    /// <summary>
    /// Returns true only when actively Heating or Cooling.
    /// </summary>
    public override bool IsOn() =>
        State == ThermostatState.Heating || State == ThermostatState.Cooling;
}