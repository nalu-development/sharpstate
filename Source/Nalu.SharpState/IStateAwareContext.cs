namespace Nalu.SharpState;

/// <summary>
/// Optional capability for the machine <strong>context</strong> object: the engine detects this interface at runtime
/// (<c>context as <see cref="IStateAwareContext{TState}"/></c>) and forwards each committed leaf-state change to
/// <see cref="OnStateChanged"/>.
/// </summary>
/// <typeparam name="TState">The machine's state enum type (same <c>TState</c> as <see cref="StateMachineEngine{TContext, TArgs, TState, TTrigger, TActor}"/>).</typeparam>
/// <remarks>
/// <para>
/// <see cref="OnStateChanged"/> is invoked only after an <strong>external</strong> transition that updates the current leaf state:
/// exit actions, the transition's synchronous action, assignment of the new leaf, and entry actions have already completed.
/// It runs immediately before the engine raises <see cref="StateMachineEngine{TContext, TArgs, TState, TTrigger, TActor}.StateChanged"/>.
/// </para>
/// <para>
/// The value is always the <strong>resolved leaf</strong> state (see <see cref="StateMachineDefinition{TContext, TArgs, TState, TTrigger, TActor}.LeafOf(TState)"/>),
/// including for hierarchical machines.
/// </para>
/// <para>
/// Internal transitions, self-transitions that resolve to the same leaf, and unhandled triggers do not call <see cref="OnStateChanged"/>.
/// </para>
/// </remarks>
public interface IStateAwareContext<TState>
    where TState : struct, Enum
{
    /// <summary>
    /// Called by the engine when the active leaf state has changed to <paramref name="state"/>.
    /// </summary>
    /// <param name="state">The new current leaf state after the transition.</param>
    void OnStateChanged(TState state);
}
