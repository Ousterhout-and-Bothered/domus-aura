using SmartHome.Domain.Common;
using SmartHome.Domain.Device.StateMachine;

namespace SmartHome.Domain.Device.Thermostat;

/// <summary>
/// Represents a smart thermostat that monitors and controls the ambient temperature
/// of a location. Supports heating, cooling, and auto modes.
/// </summary>
/// <remarks>
/// <para>
/// The thermostat distinguishes between Off and Idle. Idle means the thermostat is powered
/// on but not actively heating or cooling. For filtering and reporting purposes, only
/// Heating and Cooling are considered active "on" states.
/// </para>
/// <para>
/// As of the implicit-power-on refactor, calling <see cref="SetDesiredTemperature"/> on
/// an Off thermostat no longer throws. The command layer
/// (<c>SetDesiredTemperatureCommand</c>) is responsible for invoking <see cref="TurnOn"/>
/// and ensuring the mode is appropriate for reaching the target before delegating here.
/// <see cref="SetMode"/> retains its guard because mode changes are an explicit user
/// operation and silently powering the thermostat on inside that call would obscure
/// caller intent.
/// </para>
/// </remarks>
public sealed class Thermostat : TickableDevice, IThermostatControllable, IPowerable
{
    /// <inheritdoc />
    /// <remarks>
    /// Returns <see cref="PowerState.On"/> only when the thermostat is actively heating or cooling.
    /// Idle is reported as <see cref="PowerState.Off"/> for filtering and reporting purposes.
    /// </remarks>
    public PowerState PowerState => IsOn() ? PowerState.On : PowerState.Off;

    /// <inheritdoc />
    /// <remarks>
    /// Returns <c>true</c> for Idle, Heating, and Cooling because those states mean
    /// the thermostat is powered on at the domain level.
    /// </remarks>
    public bool IsPoweredOn => State != ThermostatState.Off;

    /// <summary>
    /// Gets the current operational state of the thermostat.
    /// </summary>
    public ThermostatState State { get; private set; }

    /// <summary>
    /// The current operating mode controlling heating and cooling behavior.
    /// </summary>
    private ThermostatMode _mode;

    /// <summary>
    /// Gets the current thermostat mode.
    /// </summary>
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
    /// Gets the target temperature in Fahrenheit.
    /// Values are clamped to 60–80°F.
    /// </summary>
    public int DesiredTemperature { get; private set; }

    /// <summary>
    /// Gets the current ambient temperature of the location in Fahrenheit.
    /// </summary>
    public int AmbientTemperature { get; private set; }

    private const int DefaultTemperature = 72;
    private const int MinTemperature = 60;
    private const int MaxTemperature = 80;

    private IThermostatModeStrategy _strategy = null!;
    private IThermostatStrategyProvider _strategyProvider = new ThermostatStrategyProvider();

    /// <summary>
    /// State machine enforcing legal thermostat transitions.
    /// </summary>
    private StateMachine<ThermostatState>? _stateMachine;

    /// <summary>
    /// Gets the lazily initialized thermostat state machine.
    /// </summary>
    private StateMachine<ThermostatState> Machine =>
        _stateMachine ??= BuildMachine(State);

    /// <summary>
    /// Initializes a new instance of the <see cref="Thermostat"/> class for EF Core.
    /// </summary>
    private Thermostat()
    {
        Type = DeviceType.Thermostat;
        State = ThermostatState.Off;
        Mode = ThermostatMode.Auto;
        DesiredTemperature = DefaultTemperature;
        AmbientTemperature = DefaultTemperature;
    }

    /// <summary>
    /// Initializes a new thermostat with a specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier for the thermostat.</param>
    /// <param name="name">The human-readable name of the thermostat.</param>
    /// <param name="location">The location controlled by the thermostat.</param>
    public Thermostat(Guid id, string name, string location)
        : base(id, name, location, DeviceType.Thermostat)
    {
        DesiredTemperature = DefaultTemperature;
        AmbientTemperature = DefaultTemperature;
        Mode = ThermostatMode.Auto;
        State = ThermostatState.Off;
    }

    /// <summary>
    /// Initializes a new thermostat with a specified identifier and strategy provider.
    /// </summary>
    /// <param name="id">The unique identifier for the thermostat.</param>
    /// <param name="name">The human-readable name of the thermostat.</param>
    /// <param name="location">The location controlled by the thermostat.</param>
    /// <param name="strategyProvider">The provider used to resolve thermostat mode strategies.</param>
    public Thermostat(Guid id, string name, string location, IThermostatStrategyProvider strategyProvider)
        : base(id, name, location, DeviceType.Thermostat)
    {
        _strategyProvider = strategyProvider;
        State = ThermostatState.Off;
        Mode = ThermostatMode.Auto;
        DesiredTemperature = DefaultTemperature;
        AmbientTemperature = DefaultTemperature;
    }

    /// <summary>
    /// Initializes a new thermostat with a generated identifier and strategy provider.
    /// </summary>
    /// <param name="name">The human-readable name of the thermostat.</param>
    /// <param name="location">The location controlled by the thermostat.</param>
    /// <param name="strategyProvider">The provider used to resolve thermostat mode strategies.</param>
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
    /// Powers the thermostat on from the Off state, transitions to Idle,
    /// and immediately evaluates whether heating or cooling should begin.
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
    /// Powers the thermostat off from any non-Off state.
    /// </summary>
    public void TurnOff()
    {
        TransitionTo(ThermostatState.Off);
    }

    /// <summary>
    /// Sets the operating mode and immediately re-evaluates the thermostat state.
    /// </summary>
    /// <param name="mode">The thermostat mode to apply.</param>
    /// <remarks>
    /// The off-state guard is intentional. SetMode is an explicit user operation
    /// and the caller, whether UI or command layer, must turn the thermostat on first.
    /// Auto-powering inside SetMode would silently change the device's power
    /// state in a method that does not advertise that behavior.
    /// </remarks>
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
    /// </summary>
    /// <param name="temperature">The requested desired temperature.</param>
    /// <remarks>
    /// As of the implicit-power-on refactor, the off-state guard has been removed.
    /// Callers, usually the command layer, are responsible for transitioning the thermostat
    /// to a non-Off state before invoking this method.
    /// </remarks>
    public void SetDesiredTemperature(int temperature)
    {
        // No power-state guard — command layer ensures precondition.
        DesiredTemperature = Guard.Clamp(temperature, MinTemperature, MaxTemperature);
        EvaluateState();
    }

    /// <summary>
    /// Updates the ambient temperature and re-evaluates the thermostat if it is powered on.
    /// </summary>
    /// <param name="temperature">The new ambient temperature in Fahrenheit.</param>
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
    /// </summary>
    /// <returns>
    /// <c>true</c> if the thermostat's observable state changed; otherwise, <c>false</c>.
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
    /// Returns whether the thermostat should be considered active for filtering and display.
    /// </summary>
    /// <returns>
    /// <c>true</c> when the thermostat is Heating or Cooling; otherwise, <c>false</c>.
    /// </returns>
    public override bool IsOn() =>
        State is ThermostatState.Heating or ThermostatState.Cooling;

    /// <summary>
    /// Restores the thermostat to its default configuration.
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
    /// Delegates state evaluation to the active mode strategy.
    /// </summary>
    private void EvaluateState()
    {
        if (State == ThermostatState.Off)
        {
            return;
        }

        _strategy = _strategyProvider.GetStrategy(_mode);

        var targetState = _strategy.Evaluate(AmbientTemperature, DesiredTemperature);

        if (targetState == State)
        {
            return;
        }

        TransitionTo(targetState);
    }

    /// <summary>
    /// Performs a validated state transition and synchronizes <see cref="State"/>.
    /// </summary>
    /// <param name="target">The target thermostat state.</param>
    private void TransitionTo(ThermostatState target)
    {
        if (State == target)
        {
            return;
        }

        Machine.Transition(target);
        State = Machine.CurrentState;
    }

    /// <summary>
    /// Builds the legal thermostat transition table.
    /// </summary>
    /// <param name="initialState">The initial thermostat state.</param>
    /// <returns>A state machine configured for thermostat transitions.</returns>
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
    /// Returns a log-friendly representation of the thermostat.
    /// </summary>
    /// <returns>A string containing the thermostat identity, state, mode, and temperatures.</returns>
    public override string ToString() =>
        $"{GetType().Name}(Id={Id}, Name='{Name}', Location='{Location}', " +
        $"State={State}, Mode={Mode}, Ambient={AmbientTemperature}°F, Desired={DesiredTemperature}°F)";
}