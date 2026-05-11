namespace Nalu.SharpState;

/// <summary>
/// Callback invoked by <see cref="StateMachineEngine{TContext, TArgs, TState, TTrigger, TActor}"/> after a transition commits a new
/// leaf state.
/// </summary>
/// <typeparam name="TState">Enum type listing all states of the machine.</typeparam>
/// <typeparam name="TTrigger">Enum type listing all triggers of the machine.</typeparam>
/// <typeparam name="TArgs">Machine-specific trigger argument union.</typeparam>
/// <param name="from">The leaf state the machine was in before the transition.</param>
/// <param name="to">The new leaf state the machine settled into after the transition.</param>
/// <param name="trigger">The trigger that caused the transition.</param>
/// <param name="args">The arguments originally passed to the trigger.</param>
public delegate void StateChangedHandler<TState, TTrigger, TArgs>(TState from, TState to, TTrigger trigger, TArgs args)
    where TState : struct, Enum
    where TTrigger : struct, Enum;
