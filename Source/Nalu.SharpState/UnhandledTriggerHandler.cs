namespace Nalu.SharpState;

/// <summary>
/// Callback invoked by <see cref="StateMachineEngine{TContext, TArgs, TState, TTrigger, TActor}"/> when a trigger fires but no
/// transition matches on the current leaf state nor any of its ancestors.
/// </summary>
/// <typeparam name="TState">Enum type listing all states of the machine.</typeparam>
/// <typeparam name="TTrigger">Enum type listing all triggers of the machine.</typeparam>
/// <typeparam name="TArgs">Machine-specific trigger argument union.</typeparam>
/// <param name="currentState">The current leaf state that could not process <paramref name="trigger"/>.</param>
/// <param name="trigger">The trigger that was fired without a matching transition.</param>
/// <param name="args">The arguments originally passed to the trigger.</param>
public delegate void UnhandledTriggerHandler<TState, TTrigger, TArgs>(TState currentState, TTrigger trigger, TArgs args)
    where TState : struct, Enum
    where TTrigger : struct, Enum;
