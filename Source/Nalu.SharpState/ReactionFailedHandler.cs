namespace Nalu.SharpState;

/// <summary>
/// Callback invoked when post-transition asynchronous work fails after the state transition has already completed
/// (<c>WhenExitedAsync</c>, <c>WhenEnteredAsync</c>, <c>ReactAsync</c>).
/// </summary>
/// <typeparam name="TState">Enum type listing all states of the machine.</typeparam>
/// <typeparam name="TTrigger">Enum type listing all triggers of the machine.</typeparam>
/// <typeparam name="TArgs">Machine-specific trigger argument union.</typeparam>
/// <param name="from">The committed source leaf state.</param>
/// <param name="to">The committed destination leaf state.</param>
/// <param name="trigger">The trigger that produced the post-transition asynchronous work.</param>
/// <param name="args">The arguments originally passed to the trigger.</param>
/// <param name="exception">The exception thrown by post-transition asynchronous work.</param>
public delegate void ReactionFailedHandler<in TState, in TTrigger, in TArgs>(
    TState from,
    TState to,
    TTrigger trigger,
    TArgs args,
    Exception exception)
    where TState : struct, Enum
    where TTrigger : struct, Enum;
