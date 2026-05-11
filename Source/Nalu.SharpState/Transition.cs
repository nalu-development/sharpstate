namespace Nalu.SharpState;

/// <summary>
/// A single resolved transition built by the trigger fluent builders.
/// Carries the optional guard, optional synchronous transition action, optional asynchronous reaction,
/// and either a target state (external transition) or the <see cref="IsInternal"/> flag
/// (internal transition that does not leave the current state).
/// </summary>
/// <typeparam name="TContext">Type of the user-supplied context carried by the machine.</typeparam>
/// <typeparam name="TArgs">Machine-specific trigger argument union.</typeparam>
/// <typeparam name="TState">Enum type listing all states of the machine.</typeparam>
/// <typeparam name="TActor">Type of the actor passed into post-transition reactions.</typeparam>
public sealed class Transition<TContext, TArgs, TState, TActor>
    where TState : struct, Enum
{
    internal Transition(
        TState target,
        Func<TContext, IServiceProvider, TArgs, TState>? targetSelector,
        bool isInternal,
        Func<TContext, IServiceProvider, TArgs, bool>? guard,
        List<string>? guardLabels,
        Action<TContext, IServiceProvider, TArgs>? syncAction,
        Func<TActor, TContext, IServiceProvider, TArgs, ValueTask>? reactionAsync,
        (TState Target, string Label)[]? dynamicTargetHints = null)
    {
        Target = target;
        TargetSelector = targetSelector;
        IsInternal = isInternal;
        Guard = guard;
        GuardLabels = guardLabels;
        SyncAction = syncAction;
        ReactionAsync = reactionAsync;
        DynamicTargetHints = dynamicTargetHints;
    }

    /// <summary>
    /// When <c>true</c>, the transition does not change the current state; only the action runs.
    /// When <c>false</c>, <see cref="Target"/> is the destination state.
    /// </summary>
    public bool IsInternal { get; }

    /// <summary>
    /// The destination state of this external transition.
    /// Accessing this on an internal transition throws <see cref="InvalidOperationException"/>.
    /// </summary>
    public TState Target
        => IsInternal
            ? throw new InvalidOperationException("Internal transitions do not have a target state.")
            : TargetSelector is not null
                ? throw new InvalidOperationException("Dynamic transitions resolve their target state at dispatch time.")
                : field;

    /// <summary>
    /// Optional target selector resolved at dispatch time before any state change is committed.
    /// </summary>
    public Func<TContext, IServiceProvider, TArgs, TState>? TargetSelector { get; }

    /// <summary>
    /// When this transition uses <see cref="TargetSelector"/>, optional labeled target hints from the dynamic
    /// <c>TransitionTo(..., params (TState Target, string Label)[])</c> overloads—documentation only
    /// (for example Graphviz or Mermaid export). Does not affect runtime resolution.
    /// </summary>
    public (TState Target, string Label)[]? DynamicTargetHints { get; }

    /// <summary>
    /// Optional guard predicate. When <c>null</c>, the transition always fires.
    /// When non-null, the transition fires only if the guard returns <c>true</c>.
    /// </summary>
    public Func<TContext, IServiceProvider, TArgs, bool>? Guard { get; }

    /// <summary>
    /// Optional ordered labels from <c>When(..., label)</c> calls only (non-null labels, in declaration order).
    /// <see langword="null"/> when no label was supplied for any contributing guard.
    /// </summary>
    public List<string>? GuardLabels { get; }

    /// <summary>
    /// Optional synchronous action executed after the guard passes and before the state change is committed.
    /// </summary>
    public Action<TContext, IServiceProvider, TArgs>? SyncAction { get; }

    /// <summary>
    /// Optional asynchronous reaction scheduled after the state transition has completed.
    /// </summary>
    public Func<TActor, TContext, IServiceProvider, TArgs, ValueTask>? ReactionAsync { get; }
}
