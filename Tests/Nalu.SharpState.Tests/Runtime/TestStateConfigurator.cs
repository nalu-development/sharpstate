namespace Nalu.SharpState.Tests.Runtime;

internal sealed class TestStateConfigurator<TContext, TServiceProvider, TState, TTrigger, TActor>
    : StateConfigurator<TContext, TServiceProvider, TState, TTrigger, TActor>
    where TContext : class
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    public TestStateConfigurator<TContext, TServiceProvider, TState, TTrigger, TActor> On(
        TTrigger trigger,
        Transition<TContext, TServiceProvider, TState, TActor> transition)
    {
        AddTransitions(trigger, [transition]);
        return this;
    }

    public TestStateConfigurator<TContext, TServiceProvider, TState, TTrigger, TActor> Parent(TState parent)
    {
        SetParent(parent);
        return this;
    }

    public TestStateConfigurator<TContext, TServiceProvider, TState, TTrigger, TActor> AsStateMachine(TState initial)
    {
        SetInitialChild(initial);
        return this;
    }

    public TestStateConfigurator<TContext, TServiceProvider, TState, TTrigger, TActor> WhenEntering(Action<TContext> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetEntryAction((ctx, _) => action(ctx));
        return this;
    }

    public TestStateConfigurator<TContext, TServiceProvider, TState, TTrigger, TActor> WhenEntering(Action<TContext, TServiceProvider> action)
    {
        SetEntryAction(action);
        return this;
    }

    public TestStateConfigurator<TContext, TServiceProvider, TState, TTrigger, TActor> WhenExiting(Action<TContext> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetExitAction((ctx, _) => action(ctx));
        return this;
    }

    public TestStateConfigurator<TContext, TServiceProvider, TState, TTrigger, TActor> WhenExiting(Action<TContext, TServiceProvider> action)
    {
        SetExitAction(action);
        return this;
    }
}

internal static class TestTransition
{
    public static Transition<TContext, TServiceProvider, TState, TActor> ToTarget<TContext, TServiceProvider, TState, TActor>(
        TState target,
        Func<TContext, TServiceProvider, TriggerArgs, bool>? guard = null,
        List<string>? guardLabels = null,
        Action<TContext, TServiceProvider, TriggerArgs>? syncAction = null,
        Func<TActor, TContext, TServiceProvider, TriggerArgs, ValueTask>? reactionAsync = null)
        where TContext : class
        where TState : struct, Enum
        => new(target, null, false, guard, guardLabels, syncAction, reactionAsync);

    public static Transition<TContext, TServiceProvider, TState, TActor> ToDynamicTarget<TContext, TServiceProvider, TState, TActor>(
        Func<TContext, TServiceProvider, TriggerArgs, TState> targetSelector,
        Func<TContext, TServiceProvider, TriggerArgs, bool>? guard = null,
        List<string>? guardLabels = null,
        Action<TContext, TServiceProvider, TriggerArgs>? syncAction = null,
        Func<TActor, TContext, TServiceProvider, TriggerArgs, ValueTask>? reactionAsync = null,
        params (TState Target, string Label)[] targetHints)
        where TContext : class
        where TState : struct, Enum
        => new(
            default!,
            targetSelector,
            false,
            guard,
            guardLabels,
            syncAction,
            reactionAsync,
            targetHints.Length > 0 ? targetHints : null);

    public static Transition<TContext, TServiceProvider, TState, TActor> Stay<TContext, TServiceProvider, TState, TActor>(
        Action<TContext, TServiceProvider, TriggerArgs>? syncAction = null,
        Func<TActor, TContext, TServiceProvider, TriggerArgs, ValueTask>? reactionAsync = null,
        Func<TContext, TServiceProvider, TriggerArgs, bool>? guard = null,
        List<string>? guardLabels = null)
        where TContext : class
        where TState : struct, Enum
        => new(default!, null, true, guard, guardLabels, syncAction, reactionAsync);
}

internal sealed class TestActor;
