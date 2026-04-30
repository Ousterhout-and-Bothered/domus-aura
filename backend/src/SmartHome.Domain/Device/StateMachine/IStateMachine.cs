namespace SmartHome.Domain.Device.StateMachine;

/// <summary>
/// Generic state machine contract for domain entities that transition between
/// a finite set of states. Implementations own the table of allowed transitions
/// and throw when invalid transitions are attempted.
/// </summary>
/// <typeparam name="TState">
/// The state type — typically an enum (e.g. <c>PowerState</c>, <c>DoorLockState</c>).
/// Must be a non-null value type so transitions compare by value, not reference.
/// </typeparam>
public interface IStateMachine<TState>
    where TState : struct
{
    /// <summary>
    /// The machine's current state.
    /// </summary>
    TState CurrentState { get; }

    /// <summary>
    /// Returns true if transitioning from <see cref="CurrentState"/> to
    /// <paramref name="target"/> is permitted by the transition table.
    /// </summary>
    bool CanTransition(TState target);

    /// <summary>
    /// Transitions the machine to <paramref name="target"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the transition from <see cref="CurrentState"/> to
    /// <paramref name="target"/> is not permitted.
    /// </exception>
    void Transition(TState target);
}