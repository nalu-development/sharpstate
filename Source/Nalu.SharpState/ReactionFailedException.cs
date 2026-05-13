namespace Nalu.SharpState;

/// <summary>
/// Thrown when post-transition asynchronous work (for example <c>WhenExitedAsync</c>, <c>WhenEnteredAsync</c>, or <c>ReactAsync</c>)
/// fails after the synchronous transition has already committed and the new state is visible.
/// </summary>
public sealed class ReactionFailedException : Exception
{
    /// <summary>
    /// The committed source leaf state when the failure occurred.
    /// </summary>
    public object? SourceState { get; }

    /// <summary>
    /// The committed destination leaf state when the failure occurred.
    /// </summary>
    public object? DestinationState { get; }

    /// <summary>
    /// The trigger that caused the transition.
    /// </summary>
    public object? Trigger { get; }

    /// <summary>
    /// The trigger arguments union passed to the trigger.
    /// </summary>
    public object? TriggerArgs { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactionFailedException"/> class.
    /// </summary>
    /// <param name="sourceState">The committed source leaf state.</param>
    /// <param name="destinationState">The committed destination leaf state.</param>
    /// <param name="trigger">The trigger that fired.</param>
    /// <param name="triggerArgs">The trigger arguments.</param>
    /// <param name="innerException">The exception thrown by post-transition asynchronous work.</param>
    public ReactionFailedException(
        object? sourceState,
        object? destinationState,
        object? trigger,
        object? triggerArgs,
        Exception innerException)
        : base(
            "Post-transition asynchronous work failed after the state change was already committed.",
            innerException)
    {
        SourceState = sourceState;
        DestinationState = destinationState;
        Trigger = trigger;
        TriggerArgs = triggerArgs;
    }
}
