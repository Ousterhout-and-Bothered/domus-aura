using SmartHome.Domain.Device.StateMachine;
using SmartHome.Domain.Common;

namespace SmartHome.Domain.Device.Thermostat;

/// <summary>
/// Represents a smart thermostat that monitors and controls the ambient temperature
/// of a location. Supports heating, cooling, and auto modes.
/// Transitions between Off, Idle, Heating, and Cooling are enforced by a state machine,
/// while the active <see cref="IThermostatModeStrategy"/> determines which target state
/// is appropriate for the current conditions.
/// </summary>
public sealed class Thermostat : TickableDevice, IThermostatControllable, IPowerable
{
    /// <inheritdoc />
    public PowerState PowerState => IsOn() ? PowerState.On : PowerState.Off;

    /// <summary>
    /// The current operational state of the thermostat.
    /// </summary>
    public ThermostatState State { get; private set; }

    /// <summary>
    /// The current operating mode controlling heating and cooling behavior.
    /// Updating the mode also refreshes the active evaluation strategy so
    /// EF Core rehydration produces a consistent runtime state.
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
    /// The target temperature in Fahrenheit. Values are clamped to 60–80°F.
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
    /// same invariants as freshly constructed instances.
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
    /// If the thermostat is already active, this operation has no effect.
    /// </summary>
    public void TurnOn()
    {
        if (State != ThermostatState.Off)
        {
            return;
        }

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
        Guard.AgainstInvalidState(
            State != ThermostatState.Off,
            "Mode can only be changed while the thermostat is on.");

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
        Guard.AgainstInvalidState(
            State != ThermostatState.Off,
            "Temperature can only be changed while the thermostat is on.");

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
        {
            EvaluateState();
        }
    }

    /// <summary>
    /// Advances the thermostat by one simulation tick.
    /// When heating, the ambient temperature increases by 1°F.
    /// When cooling, the ambient temperature decreases by 1°F.
    /// When Off or Idle, the tick has no effect.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the thermostat's observable state changed during the tick;
    /// otherwise, <c>false</c>.
    /// </returns>
    public override bool Tick()
    {
        if (State == ThermostatState.Off || State == ThermostatState.Idle)
        {
            return false;
        }

        var previousAmbientTemperature = AmbientTemperature;
        var previousState = State;

        if (State == ThermostatState.Heating && AmbientTemperature < DesiredTemperature)
        {
            AmbientTemperature++;
        }
        else if (State == ThermostatState.Cooling && AmbientTemperature > DesiredTemperature)
        {
            AmbientTemperature--;
        }

        EvaluateState();

        return AmbientTemperature != previousAmbientTemperature ||
               State != previousState;
    }

    /// <summary>
    /// Delegates state evaluation to the active mode strategy and transitions
    /// through the state machine if a different target state is required.
    /// </summary>
    private void EvaluateState()
    {
        if (State == ThermostatState.Off)
        {
            return;
        }

        var targetState = _strategy.Evaluate(AmbientTemperature, DesiredTemperature);

        if (targetState == State)
        {
            return;
        }

        TransitionTo(targetState);
    }

    /// <summary>
    /// Performs a validated state transition and keeps <see cref="State"/>
    /// synchronized with the underlying state machine.
    /// </summary>
    private void TransitionTo(ThermostatState target)
    {
        Machine.Transition(target);
        State = Machine.CurrentState;
    }

    /// <summary>
    /// Returns <c>true</c> only when actively heating or cooling.
    /// </summary>
    public override bool IsOn() =>
        State == ThermostatState.Heating || State == ThermostatState.Cooling;

    /// <summary>
    /// Restores the thermostat to its default configuration and resets the
    /// transition state machine to match the default Off state.
    /// </summary>
    public override void ResetToDefaults()
    {
        Mode = ThermostatMode.Auto;
        DesiredTemperature = DefaultTemperature;
        AmbientTemperature = DefaultTemperature;
        State = ThermostatState.Off;
        _stateMachine = BuildMachine(State);
    }

    /// <summary>
    /// Builds the legal thermostat transition table.
    /// Off transitions only to Idle.
    /// Idle transitions to Off, Heating, or Cooling.
    /// Heating and Cooling may transition to Off, Idle, or each other.
    /// Self-transitions are intentionally excluded.
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
    /// Returns a log-friendly representation including operational state,
    /// mode, and both temperatures.
    /// </summary>
    public override string ToString() =>
        $"{GetType().Name}(Id={Id}, Name='{Name}', Location='{Location}', " +
        $"State={State}, Mode={Mode}, Ambient={AmbientTemperature}°F, Desired={DesiredTemperature}°F)";
}