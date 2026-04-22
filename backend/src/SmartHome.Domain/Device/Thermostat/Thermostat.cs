using SmartHome.Domain.Device.StateMachine;
using SmartHome.Domain.Common;

namespace SmartHome.Domain.Device.Thermostat;

/// <summary>
/// Represents a smart thermostat that monitors and controls the ambient temperature
/// of a location. Supports heating, cooling, and auto modes.
/// Transitions between Off, Idle, Heating, and Cooling are enforced by a state machine;
/// which target state is appropriate at any given moment is decided by the active
/// <see cref="IThermostatModeStrategy"/>.
/// </summary>
public sealed class Thermostat : TickableDevice, IThermostatControllable, IPowerable
{
    /// <inheritdoc />
    public PowerState PowerState => State == ThermostatState.Off ? PowerState.Off : PowerState.On;

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
            Guard.EnumDefined(value, nameof(value));

            _mode = value;
            _strategy = _strategyProvider.GetStrategy(value);
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
    private IThermostatStrategyProvider _strategyProvider = new ThermostatStrategyProvider();

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
        Mode = ThermostatMode.Auto; // Ensure mode and strategy are initialized
        DesiredTemperature = DefaultTemperature;
        AmbientTemperature = DefaultTemperature;
    }

    public Thermostat(string name, string location)
        : this(name, location, new ThermostatStrategyProvider())
    {
    }

    public Thermostat(string name, string location, IThermostatStrategyProvider strategyProvider)
        : base(name, location, DeviceType.Thermostat)
    {
        _strategyProvider = strategyProvider;
        State = ThermostatState.Off;
        Mode = ThermostatMode.Auto;
        DesiredTemperature = DefaultTemperature;
        AmbientTemperature = DefaultTemperature;
    }

    /// <summary>
    /// Powers the thermostat on from the Off state, transitioning to Idle and
    /// immediately evaluating whether heating or cooling should begin.
    /// If the thermostat is already in an active state (Idle, Heating, or Cooling),
    /// this operation ensures the current state is still appropriate.
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
    /// Throws <see cref="InvalidOperationException"/> if the thermostat is off.
    /// </summary>
    public void SetMode(ThermostatMode mode)
    {
        Guard.AgainstInvalidState(State != ThermostatState.Off, "Mode can only be changed while the thermostat is on.");

        Mode = mode;
        EvaluateState();
    }

    /// <summary>
    /// Sets the desired temperature in Fahrenheit.
    /// Values outside 60–80°F are clamped to the nearest bound.
    /// Throws <see cref="InvalidOperationException"/> if the thermostat is off.
    /// </summary>
    public void SetDesiredTemperature(int temperature)
    {
        Guard.AgainstInvalidState(State != ThermostatState.Off, "Temperature can only be changed while the thermostat is on.");
        
        DesiredTemperature = Guard.Clamp(temperature, MinTemperature, MaxTemperature);
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
    public override void Tick()
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