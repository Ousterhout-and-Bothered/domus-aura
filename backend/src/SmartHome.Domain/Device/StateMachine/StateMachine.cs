using SmartHome.Domain.Common;

namespace SmartHome.Domain.Device.StateMachine;

/// <summary>
/// Default state machine. Configured with an allowed-transitions table at
/// construction and rejects any transition not in the table. Enforces
/// invariants by throwing — never silently ignores invalid transitions.
/// </summary>
/// <typeparam name="TState">
/// The state type — typically an enum. See <see cref="IStateMachine{TState}"/>.
/// </typeparam>
public sealed class StateMachine<TState> : IStateMachine<TState>
    where TState : struct
{
    private readonly IReadOnlyDictionary<TState, IReadOnlySet<TState>> _allowedTransitions;
    private TState _currentState;

    /// <summary>
    /// Constructs a state machine starting at <paramref name="initialState"/>
    /// with the provided allowed-transitions table.
    /// </summary>
    /// <param name="initialState">The state the machine starts in.</param>
    /// <param name="allowedTransitions">
    /// Maps each possible source state to the set of target states that are
    /// legal to transition to. A state not present in the map is treated as
    /// "no transitions allowed out of this state."
    /// </param>
    public StateMachine(
        TState initialState,
        IReadOnlyDictionary<TState, IReadOnlySet<TState>> allowedTransitions)
    {
        _currentState = initialState;
        _allowedTransitions = allowedTransitions;
    }

    /// <summary>
    /// Gets the current state the machine is in.
    /// </summary>
    public TState CurrentState => _currentState;

    /// <summary>
    /// Checks if a transition to the target state is allowed from the current state.
    /// </summary>
    /// <param name="target">The target state to check.</param>
    /// <returns>True if the transition is permitted; otherwise, false.</returns>
    public bool CanTransition(TState target) =>
        _allowedTransitions.TryGetValue(_currentState, out var allowed)
        && allowed.Contains(target);

    /// <summary>
    /// Performs a state transition.
    /// Throws <see cref="SmartHome.Domain.Common.Exceptions.InvalidDomainOperationException"/> if the transition is illegal.
    /// </summary>
    /// <param name="target">The target state to transition to.</param>
    public void Transition(TState target)
    {
        if (!CanTransition(target))
        {
            Guard.ThrowIfInvalidTransition(_currentState, target, _allowedTransitions);
        }

        _currentState = target;
    }

    /// <summary>
    /// Forces the machine into <paramref name="target"/> bypassing the
    /// transition table. Intended for persistence-layer rehydration and
    /// reset-to-defaults flows where the normal transition rules don't apply.
    /// </summary>
    internal void ForceState(TState target) => _currentState = target;
}