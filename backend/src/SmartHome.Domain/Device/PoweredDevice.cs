using SmartHome.Domain.Device.StateMachine;

namespace SmartHome.Domain.Device;

/// <summary>
/// Base class for all devices that have an explicit binary power state.
/// Transitions between <see cref="PowerState.On"/> and
/// <see cref="PowerState.Off"/> are enforced by a reusable state machine.
/// </summary>
/// <remarks>
/// Device-specific behavior should be implemented by subclasses through
/// <see cref="OnPoweredOn"/> and <see cref="OnPoweredOff"/> rather than by
/// external commands checking concrete device types.
/// </remarks>
public abstract class PoweredDevice : Device, IPowerable
{
    /// <summary>
    /// Gets the current externally visible power state of the device.
    /// </summary>
    public PowerState PowerState { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the device is currently powered on
    /// at the domain level.
    /// </summary>
    public bool IsPoweredOn => PowerState == PowerState.On;

    /// <summary>
    /// State machine enforcing legal power transitions. This field is not persisted.
    /// It is rebuilt from <see cref="PowerState"/> on first use so EF Core-rehydrated
    /// instances honor the same invariants as freshly constructed instances.
    /// </summary>
    private StateMachine<PowerState>? _stateMachine;

    /// <summary>
    /// Gets the lazily initialized power state machine.
    /// </summary>
    private StateMachine<PowerState> Machine =>
        _stateMachine ??= BuildMachine(PowerState);

    /// <summary>
    /// Initializes a new instance of the <see cref="PoweredDevice"/> class for EF Core.
    /// </summary>
    protected PoweredDevice()
    {
        PowerState = PowerState.Off;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PoweredDevice"/> class
    /// with a specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier for the device.</param>
    /// <param name="name">The human-readable device name.</param>
    /// <param name="location">The room or area where the device is located.</param>
    /// <param name="type">The concrete device type.</param>
    protected PoweredDevice(Guid id, string name, string location, DeviceType type)
        : base(id, name, location, type)
    {
        PowerState = PowerState.Off;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PoweredDevice"/> class
    /// with a generated identifier.
    /// </summary>
    /// <param name="name">The human-readable device name.</param>
    /// <param name="location">The room or area where the device is located.</param>
    /// <param name="type">The concrete device type.</param>
    protected PoweredDevice(string name, string location, DeviceType type)
        : base(name, location, type)
    {
        PowerState = PowerState.Off;
    }

    /// <summary>
    /// Powers the device on if it is currently off.
    /// If the device is already on, the operation is treated as an idempotent no-op.
    /// </summary>
    public virtual void TurnOn()
    {
        if (PowerState == PowerState.On)
        {
            return;
        }

        Machine.Transition(PowerState.On);
        PowerState = Machine.CurrentState;

        OnPoweredOn();
    }

    /// <summary>
    /// Powers the device off if it is currently on.
    /// If the device is already off, the operation is treated as an idempotent no-op.
    /// </summary>
    public virtual void TurnOff()
    {
        if (PowerState == PowerState.Off)
        {
            return;
        }

        Machine.Transition(PowerState.Off);
        PowerState = Machine.CurrentState;

        OnPoweredOff();
    }

    /// <summary>
    /// Allows subclasses to apply device-specific behavior after the device powers on.
    /// </summary>
    protected virtual void OnPoweredOn()
    {
    }

    /// <summary>
    /// Allows subclasses to apply device-specific behavior after the device powers off.
    /// </summary>
    protected virtual void OnPoweredOff()
    {
    }

    /// <summary>
    /// Returns whether the device should be considered on for filtering and display.
    /// </summary>
    /// <returns>
    /// <c>true</c> when the device's visible power state is
    /// <see cref="PowerState.On"/>; otherwise, <c>false</c>.
    /// </returns>
    public override bool IsOn() => PowerState == PowerState.On;

    /// <summary>
    /// Restores the powered device to its default state and rebuilds its state machine.
    /// </summary>
    public override void ResetToDefaults()
    {
        ResetPoweredDefaults();

        PowerState = PowerState.Off;
        _stateMachine = BuildMachine(PowerState);
    }

    /// <summary>
    /// Resets subclass-specific attributes to their default values.
    /// </summary>
    protected abstract void ResetPoweredDefaults();

    /// <summary>
    /// Builds the legal transition table for a powered device.
    /// </summary>
    /// <param name="initialState">The initial power state for the state machine.</param>
    /// <returns>A state machine configured for powered-device transitions.</returns>
    private static StateMachine<PowerState> BuildMachine(PowerState initialState) =>
        new(initialState, new Dictionary<PowerState, IReadOnlySet<PowerState>>
        {
            [PowerState.Off] = new HashSet<PowerState>
            {
                PowerState.On
            },
            [PowerState.On] = new HashSet<PowerState>
            {
                PowerState.Off
            }
        });
}