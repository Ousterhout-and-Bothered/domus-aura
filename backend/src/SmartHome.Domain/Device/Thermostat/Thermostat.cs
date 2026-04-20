using SmartHome.Domain.Device.StateMachine;

namespace SmartHome.Domain.Device.Thermostat;

/// <summary>
/// Represents a smart thermostat that monitors and controls the ambient temperature
/// of a location. Supports heating, cooling, and auto modes.
/// Transitions between Off, Idle, Heating, and Cooling are enforced by a state machine;
/// which target state is appropriate at any given moment is decided by the active
/// <see cref="IThermostatModeStrategy"/>.
/// </summary>
public sealed class Thermostat : Device, IThermostatControllable, ITickable
{
    /// <summary>
    /// The current state of the thermostat.
    /// </summary>
    public ThermostatState State { get; private set; }

    /// <summary>
    /// The current mode controlling heating/cooling behavior.
    /// When set, automatically updates the active strategy so EF Core
    /// rehydration always produces a consistent state.
    /// </summary>
    private ThermostatMode _mode;

    public ThermostatMode Mode
    {
        get => _mode;
        private set
        {
            if (!Enum.IsDefined<ThermostatMode>(value))
                throw new ArgumentOutOfRangeException(nameof(value),
                    $"Unsupported thermostat mode: {value}.");

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

    // To add a new mode: implement IThermostatModeStrategy and add an entry here.
    // Thermostat's core logic never changes — Open/Closed Principle.
    private static readonly Dictionary<ThermostatMode, IThermostatModeStrategy> Strategies = new()
    {
        { ThermostatMode.Heat, new HeatModeStrategy() },
        { ThermostatMode.Cool, new CoolModeStrategy() },
        { ThermostatMode.Auto, new AutoModeStrategy() }
    };

    /// <summary>
    /// State machine enforcing legal transitions. Not persisted — rebuilt from
    /// <see cref="State"/> on first use so EF-rehydrated instances honor the
    /// same invariants as freshly-constructed ones.
    /// </summary>
    private StateMachine<ThermostatState>? _stateMachine;

    private StateMachine<ThermostatState> Machine =>
        _stateMachine ??= BuildMachine(State);

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
        TransitionTo(ThermostatState.Idle);
        EvaluateState();
    }

    /// <summary>
    /// Powers the thermostat off from any active state.
    /// </summary>
    public void TurnOff()
    {
        TransitionTo(ThermostatState.Off);
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
    /// Delegates state evaluation to the active mode strategy and,
    /// if the strategy wants a different state, transitions to it
    /// through the state machine.
    /// </summary>
    private void EvaluateState()
    {
        if (State == ThermostatState.Off)
            return;

        var targetState = _strategy.Evaluate(AmbientTemperature, DesiredTemperature);

        // No-op if strategy returns the current state — the state machine
        // rejects identity transitions, so we short-circuit here.
        if (targetState == State)
            return;

        TransitionTo(targetState);
    }

    /// <summary>
    /// Validated transition helper. Uses the state machine to enforce invariants
    /// and keeps the public <see cref="State"/> property synchronized.
    /// </summary>
    private void TransitionTo(ThermostatState target)
    {
        Machine.Transition(target);
        State = Machine.CurrentState;
    }

    /// <summary>
    /// Returns true only when actively Heating or Cooling.
    /// </summary>
    public override bool IsOn() =>
        State == ThermostatState.Heating || State == ThermostatState.Cooling;

    public override void ResetToDefaults()
    {
        // Reset is privileged — bypass the transition table entirely so a
        // thermostat in any state (Heating, Cooling, Idle) collapses to Off
        // in one step without needing intermediate transitions.
        Mode = ThermostatMode.Auto;
        DesiredTemperature = DefaultTemperature;
        AmbientTemperature = DefaultTemperature;
        State = ThermostatState.Off;
        _stateMachine = BuildMachine(State);
    }

    /// <summary>
    /// Builds the transition table for a thermostat.
    /// Rules:
    ///   Off      → Idle only (entered via TurnOn).
    ///   Idle     → Off, Heating, Cooling.
    ///   Heating  → Off, Idle, Cooling (Cooling added for Auto mode swings).
    ///   Cooling  → Off, Idle, Heating.
    /// Self-transitions are never in the table — that's how we preserve the
    /// "already on / already off" rejection semantics from the original design.
    /// </summary>
    private static StateMachine<ThermostatState> BuildMachine(ThermostatState initialState) =>
        new(initialState, new Dictionary<ThermostatState, IReadOnlySet<ThermostatState>>
        {
            [ThermostatState.Off] = new HashSet<ThermostatState>
            {
                ThermostatState.Idle
            },
            [ThermostatState.Idle] = new HashSet<ThermostatState>
            {
                ThermostatState.Off,
                ThermostatState.Heating,
                ThermostatState.Cooling
            },
            [ThermostatState.Heating] = new HashSet<ThermostatState>
            {
                ThermostatState.Off,
                ThermostatState.Idle,
                ThermostatState.Cooling
            },
            [ThermostatState.Cooling] = new HashSet<ThermostatState>
            {
                ThermostatState.Off,
                ThermostatState.Idle,
                ThermostatState.Heating
            }
        });

    /// <summary>
    /// Log-friendly representation including operational state, mode, and both temperatures.
    /// </summary>
    public override string ToString() =>
        $"{GetType().Name}(Id={Id}, Name='{Name}', Location='{Location}', " +
        $"State={State}, Mode={Mode}, Ambient={AmbientTemperature}°F, Desired={DesiredTemperature}°F)";
}