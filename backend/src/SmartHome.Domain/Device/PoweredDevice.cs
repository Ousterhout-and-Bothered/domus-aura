using SmartHome.Domain.Device.StateMachine;

namespace SmartHome.Domain.Device;

/// <summary>
/// Base class for all devices that have an explicit power state.
/// Transitions between On and Off are enforced by a state machine;
/// subclasses can react to transitions via <see cref="OnPoweredOn"/>
/// and <see cref="OnPoweredOff"/> hooks.
/// </summary>
public abstract class PoweredDevice : Device, IPowerable
{
    /// <summary>
    /// Current power state of the device. Exposed for persistence (EF Core)
    /// and queries; mutations flow through <see cref="TurnOn"/> / <see cref="TurnOff"/>.
    /// </summary>
    public PowerState PowerState { get; private set; }

    /// <summary>
    /// State machine enforcing legal transitions. Not persisted — rebuilt from
    /// <see cref="PowerState"/> on first use so EF-rehydrated instances
    /// honor the same invariants as freshly-constructed ones.
    /// </summary>
    private StateMachine<PowerState>? _stateMachine;

    private StateMachine<PowerState> Machine =>
        _stateMachine ??= BuildMachine(PowerState);

    // Required for EF Core — ensures proper materialization with default state
    protected PoweredDevice()
    {
        PowerState = PowerState.Off;
    }

    protected PoweredDevice(string name, string location, DeviceType type)
        : base(name, location, type)
    {
        PowerState = PowerState.Off;
    }

    public virtual void TurnOn()
    {
        Machine.Transition(PowerState.On);
        PowerState = Machine.CurrentState;

        // Allow subclasses to apply behavior on power-on (e.g., restore state).
        OnPoweredOn();
    }

    public virtual void TurnOff()
    {
        Machine.Transition(PowerState.Off);
        PowerState = Machine.CurrentState;

        // Allow subclasses to handle shutdown behavior.
        OnPoweredOff();
    }

    // Hook for subclasses — executed after device is powered on.
    protected virtual void OnPoweredOn() { }

    // Hook for subclasses — executed after device is powered off.
    protected virtual void OnPoweredOff() { }

    public override bool IsOn() => PowerState == PowerState.On;

    public override void ResetToDefaults()
    {
        // Subclasses reset their device-specific attributes first so that
        // ForceOff() below sees a clean slate.
        ResetPoweredDefaults();

        // Force power state to Off, bypassing the transition table.
        // Reset is a privileged operation — it legitimately overrides normal
        // rules (e.g., a running fan should reset to Off in one step even if
        // the machine's currently "off" state rejects Off -> Off).
        PowerState = PowerState.Off;
        _stateMachine = BuildMachine(PowerState);
    }

    // Subclasses must define how their attributes reset.
    protected abstract void ResetPoweredDefaults();

    /// <summary>
    /// Builds the transition table for a powered device. Simple two-state
    /// toggle with no identity transitions — TurnOn on an already-on device
    /// will throw, matching the prior hand-coded behavior.
    /// </summary>
    private static StateMachine<PowerState> BuildMachine(PowerState initialState) =>
        new(initialState, new Dictionary<PowerState, IReadOnlySet<PowerState>>
        {
            [PowerState.Off] = new HashSet<PowerState> { PowerState.On },
            [PowerState.On]  = new HashSet<PowerState> { PowerState.Off }
        });
}