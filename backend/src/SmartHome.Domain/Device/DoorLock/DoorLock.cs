using SmartHome.Domain.Device.StateMachine;

namespace SmartHome.Domain.Device.DoorLock;

/// <summary>
/// Represents a latch device that can be locked or unlocked.
/// DoorLocks have no power state and are always considered on.
/// Transitions between Locked and Unlocked are enforced by a state machine.
/// </summary>
public sealed class DoorLock : Device, ILockable
{
    /// <summary>
    /// The current lock state of the device. Exposed for persistence (EF Core)
    /// and queries; mutations flow through <see cref="Lock"/> / <see cref="Unlock"/>.
    /// </summary>
    public DoorLockState LockState { get; private set; }

    /// <summary>
    /// State machine enforcing legal transitions. Not persisted — rebuilt from
    /// <see cref="LockState"/> on first use so EF-rehydrated instances see
    /// the same invariants as freshly-constructed ones.
    /// </summary>
    private StateMachine<DoorLockState>? _stateMachine;

    private StateMachine<DoorLockState> Machine =>
        _stateMachine ??= BuildMachine(LockState);

    private DoorLock()
    {
        Type = DeviceType.DoorLock;
        LockState = DoorLockState.Unlocked;
    }

    public DoorLock(string name, string location)
        : base(name, location, DeviceType.DoorLock)
    {
        LockState = DoorLockState.Unlocked;
    }

    /// <summary>
    /// Locks the device.
    /// Throws <see cref="InvalidOperationException"/> if already locked.
    /// </summary>
    public void Lock()
    {
        Machine.Transition(DoorLockState.Locked);
        LockState = Machine.CurrentState;
    }

    /// <summary>
    /// Unlocks the device.
    /// Throws <see cref="InvalidOperationException"/> if already unlocked.
    /// </summary>
    public void Unlock()
    {
        Machine.Transition(DoorLockState.Unlocked);
        LockState = Machine.CurrentState;
    }

    /// <summary>
    /// Always returns true — latch devices have no power state.
    /// </summary>
    public override bool IsOn() => true;

    /// <summary>
    /// Resets the door lock to its default state.
    /// </summary>
    public override void ResetToDefaults()
    {
        // Bypass transition rules on reset — state machine would otherwise
        // reject "Locked -> Locked" or any other identity transition.
        LockState = DoorLockState.Unlocked;
        _stateMachine = BuildMachine(LockState);
    }

    /// <summary>
    /// Builds the transition table for a door lock. Kept private so the rules
    /// live alongside the class whose invariants they enforce.
    /// </summary>
    private static StateMachine<DoorLockState> BuildMachine(DoorLockState initialState) =>
        new(initialState, new Dictionary<DoorLockState, IReadOnlySet<DoorLockState>>
        {
            [DoorLockState.Locked] = new HashSet<DoorLockState> { DoorLockState.Unlocked },
            [DoorLockState.Unlocked] = new HashSet<DoorLockState> { DoorLockState.Locked }
        });

    /// <summary>
    /// Log-friendly representation including lock state.
    /// </summary>
    public override string ToString() =>
        $"{nameof(DoorLock)}(Id={Id}, Name='{Name}', Location='{Location}', LockState={LockState})";
}
