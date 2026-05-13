namespace Nalu.SharpState.Tests.Runtime;

internal sealed class TestStateConfigurator<TContext, TArgs, TState, TTrigger, TActor>
    : StateConfigurator<TContext, TArgs, TState, TTrigger, TActor>
    where TContext : class
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    public TestStateConfigurator<TContext, TArgs, TState, TTrigger, TActor> On(
        TTrigger trigger,
        Transition<TContext, TArgs, TState, TActor> transition)
    {
        AddTransitions(trigger, [transition]);
        return this;
    }

    public TestStateConfigurator<TContext, TArgs, TState, TTrigger, TActor> Parent(TState parent)
    {
        SetParent(parent);
        return this;
    }

    public TestStateConfigurator<TContext, TArgs, TState, TTrigger, TActor> AsStateMachine(TState initial)
    {
        SetInitialChild(initial);
        return this;
    }

    public TestStateConfigurator<TContext, TArgs, TState, TTrigger, TActor> WhenEntering(Action<TContext> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetEntryAction((ctx, _) => action(ctx));
        return this;
    }

    public TestStateConfigurator<TContext, TArgs, TState, TTrigger, TActor> WhenEntering(Action<TContext, IServiceProvider> action)
    {
        SetEntryAction(action);
        return this;
    }

    public TestStateConfigurator<TContext, TArgs, TState, TTrigger, TActor> WhenExiting(Action<TContext> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetExitAction((ctx, _) => action(ctx));
        return this;
    }

    public TestStateConfigurator<TContext, TArgs, TState, TTrigger, TActor> WhenExiting(Action<TContext, IServiceProvider> action)
    {
        SetExitAction(action);
        return this;
    }

    public TestStateConfigurator<TContext, TArgs, TState, TTrigger, TActor> WhenEnteredAsync(
        Func<TContext, IServiceProvider, ValueTask> action)
    {
        SetEnteredAsyncAction(action);
        return this;
    }

    public TestStateConfigurator<TContext, TArgs, TState, TTrigger, TActor> WhenExitedAsync(
        Func<TContext, IServiceProvider, ValueTask> action)
    {
        SetExitedAsyncAction(action);
        return this;
    }
}

internal static class TestTransition
{
    public static Transition<TContext, TArgs, TState, TActor> ToTarget<TContext, TArgs, TState, TActor>(
        TState target,
        Func<TContext, IServiceProvider, TArgs, bool>? guard = null,
        List<string>? guardLabels = null,
        Action<TContext, IServiceProvider, TArgs>? syncAction = null,
        Func<TActor, TContext, IServiceProvider, TArgs, ValueTask>? reactionAsync = null)
        where TContext : class
        where TState : struct, Enum
        => new(target, null, false, guard, guardLabels, syncAction, reactionAsync);

    public static Transition<TContext, TArgs, TState, TActor> ToDynamicTarget<TContext, TArgs, TState, TActor>(
        Func<TContext, IServiceProvider, TArgs, TState> targetSelector,
        Func<TContext, IServiceProvider, TArgs, bool>? guard = null,
        List<string>? guardLabels = null,
        Action<TContext, IServiceProvider, TArgs>? syncAction = null,
        Func<TActor, TContext, IServiceProvider, TArgs, ValueTask>? reactionAsync = null,
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

    public static Transition<TContext, TArgs, TState, TActor> Stay<TContext, TArgs, TState, TActor>(
        Action<TContext, IServiceProvider, TArgs>? syncAction = null,
        Func<TActor, TContext, IServiceProvider, TArgs, ValueTask>? reactionAsync = null,
        Func<TContext, IServiceProvider, TArgs, bool>? guard = null,
        List<string>? guardLabels = null)
        where TContext : class
        where TState : struct, Enum
        => new(default!, null, true, guard, guardLabels, syncAction, reactionAsync);
}

internal sealed class TestActor;

/// <summary>
/// Configurator that implements <see cref="IStateLifecycleFluent{TFluent, TContext}"/> for default-interface lifecycle tests.
/// </summary>
internal sealed class LifecycleFluentTestConfigurator
    : StateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>,
      IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext>
{
    public LifecycleFluentTestConfigurator On(FlatTrigger trigger, Transition<TestContext, IServiceProvider, FlatState, TestActor> transition)
    {
        AddTransitions(trigger, [transition]);
        return this;
    }
}
