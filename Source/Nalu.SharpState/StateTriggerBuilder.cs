#pragma warning disable CS1591

namespace Nalu.SharpState;

/// <summary>
/// Shared state for the concrete <c>StateTriggerBuilder{...}</c> variants.
/// </summary>
public abstract class StateTriggerBuilderBase<TContext, TServiceProvider, TState, TActor>
    where TContext : class
    where TState : struct, Enum
{
    private TState _target;
    private Func<TContext, TServiceProvider, TriggerArgs, TState>? _targetSelector;
    private (TState Target, string Label)[]? _dynamicTargetHints;
    private bool _hasTarget;
    private bool _stay;
    private Func<TContext, TServiceProvider, TriggerArgs, bool>? _guard;
    private List<string>? _guardLabels;
    private Action<TContext, TServiceProvider, TriggerArgs>? _syncAction;
    private Func<TActor, TContext, TServiceProvider, TriggerArgs, ValueTask>? _reactionAsync;

    protected void SetTarget(TState target)
    {
        _target = target;
        _targetSelector = null;
        _dynamicTargetHints = null;
        _hasTarget = true;
    }

    protected void SetTarget(Func<TContext, TServiceProvider, TriggerArgs, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        _target = default;
        _targetSelector = targetSelector;
        _dynamicTargetHints = targetHints.Length > 0 ? targetHints : null;
        _hasTarget = true;
    }

    protected void SetStay() => _stay = true;

    protected void SetGuard(Func<TContext, TServiceProvider, TriggerArgs, bool> guard, string? label = null)
    {
        var previous = _guard;
        _guard = previous is null
            ? guard
            : (context, sp, args) => previous(context, sp, args) && guard(context, sp, args);
        if (label is not null)
        {
            _guardLabels ??= [];
            _guardLabels.Add(label);
        }
    }

    protected void SetSyncAction(Action<TContext, TServiceProvider, TriggerArgs> action)
    {
        var previous = _syncAction;
        _syncAction = previous is null
            ? action
            : (context, sp, args) =>
            {
                previous(context, sp, args);
                action(context, sp, args);
            };
    }

    protected void SetReactionAsync(Func<TActor, TContext, TServiceProvider, TriggerArgs, ValueTask> action)
    {
        var previous = _reactionAsync;
        _reactionAsync = previous is null
            ? action
            : async (actor, context, sp, args) =>
            {
                await previous(actor, context, sp, args);
                await action(actor, context, sp, args);
            };
    }

    public void Validate()
    {
        if (_hasTarget && _stay)
        {
            throw new InvalidOperationException(
                "A transition cannot declare both a Target and Stay(). Choose one.");
        }

        if (!_hasTarget && !_stay)
        {
            throw new InvalidOperationException(
                "A transition must declare either a Target state or Stay() for an internal transition.");
        }
    }

    public IReadOnlyList<Transition<TContext, TServiceProvider, TState, TActor>> BuildTransitions()
        =>
        [
            new(
                _target,
                _targetSelector,
                _stay,
                _guard,
                _guardLabels,
                _syncAction,
                _reactionAsync,
                _dynamicTargetHints)
        ];
}

/// <summary>
/// Concrete builder for parameterless triggers.
/// </summary>
public sealed class StateTriggerBuilder<TContext, TServiceProvider, TState, TActor> :
    StateTriggerBuilderBase<TContext, TServiceProvider, TState, TActor>,
    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor>,
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor>
    where TContext : class
    where TState : struct, Enum
{
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor>.Target(TState target)
    {
        SetTarget(target);
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor>.Target(Func<TContext, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, _, _) => targetSelector(context), targetHints);
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor>.Target(Func<TContext, TServiceProvider, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, _) => targetSelector(context, sp), targetHints);
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor>.Stay()
    {
        SetStay();
        return this;
    }

    void ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor>.Ignore() => SetStay();

    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor>.When(Func<TContext, bool> guard, string? label)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, _, _) => guard(context), label);
        return this;
    }

    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor>.When(Func<TContext, TServiceProvider, bool> guard, string? label)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, _) => guard(context, sp), label);
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor> ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor>.Invoke(Action<TContext> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, _, _) => action(context));
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor> ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor>.Invoke(Action<TContext, TServiceProvider> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, _) => action(context, sp));
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor> ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor>.ReactAsync(Func<TActor, TContext, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, _, _) => action(actor, context));
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor> ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor>.ReactAsync(Func<TActor, TContext, TServiceProvider, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, _) => action(actor, context, sp));
        return this;
    }
}

/// <summary>
/// Concrete builder for single-argument triggers.
/// </summary>
public sealed class StateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0> :
    StateTriggerBuilderBase<TContext, TServiceProvider, TState, TActor>,
    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0>,
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0>
    where TContext : class
    where TState : struct, Enum
{
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0>.Target(TState target)
    {
        SetTarget(target);
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0>.Target(Func<TContext, TArg0, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, _, args) => targetSelector(context, args.Get<TArg0>(0)), targetHints);
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0>.Target(Func<TContext, TServiceProvider, TArg0, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, sp, args.Get<TArg0>(0)), targetHints);
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0>.Stay()
    {
        SetStay();
        return this;
    }

    void ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0>.Ignore() => SetStay();

    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0>.When(Func<TContext, TArg0, bool> guard, string? label)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, _, args) => guard(context, args.Get<TArg0>(0)), label);
        return this;
    }

    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0>.When(Func<TContext, TServiceProvider, TArg0, bool> guard, string? label)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, sp, args.Get<TArg0>(0)), label);
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0> ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0>.Invoke(Action<TContext, TArg0> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, _, args) => action(context, args.Get<TArg0>(0)));
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0> ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0>.Invoke(Action<TContext, TServiceProvider, TArg0> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, sp, args.Get<TArg0>(0)));
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0> ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0>.ReactAsync(Func<TActor, TContext, TArg0, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, _, args) => action(actor, context, args.Get<TArg0>(0)));
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0> ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0>.ReactAsync(Func<TActor, TContext, TServiceProvider, TArg0, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, sp, args.Get<TArg0>(0)));
        return this;
    }
}

/// <summary>
/// Concrete builder for two-argument triggers.
/// </summary>
public sealed class StateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> :
    StateTriggerBuilderBase<TContext, TServiceProvider, TState, TActor>,
    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1>,
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1>
    where TContext : class
    where TState : struct, Enum
{
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1>.Target(TState target)
    {
        SetTarget(target);
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1>.Target(Func<TContext, TArg0, TArg1, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, _, args) => targetSelector(context, args.Get<TArg0>(0), args.Get<TArg1>(1)), targetHints);
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1>.Target(Func<TContext, TServiceProvider, TArg0, TArg1, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, sp, args.Get<TArg0>(0), args.Get<TArg1>(1)), targetHints);
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1>.Stay()
    {
        SetStay();
        return this;
    }

    void ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1>.Ignore() => SetStay();

    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1>.When(Func<TContext, TArg0, TArg1, bool> guard, string? label)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, _, args) => guard(context, args.Get<TArg0>(0), args.Get<TArg1>(1)), label);
        return this;
    }

    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1>.When(Func<TContext, TServiceProvider, TArg0, TArg1, bool> guard, string? label)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, sp, args.Get<TArg0>(0), args.Get<TArg1>(1)), label);
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1>.Invoke(Action<TContext, TArg0, TArg1> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, _, args) => action(context, args.Get<TArg0>(0), args.Get<TArg1>(1)));
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1>.Invoke(Action<TContext, TServiceProvider, TArg0, TArg1> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, sp, args.Get<TArg0>(0), args.Get<TArg1>(1)));
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1>.ReactAsync(Func<TActor, TContext, TArg0, TArg1, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, _, args) => action(actor, context, args.Get<TArg0>(0), args.Get<TArg1>(1)));
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1>.ReactAsync(Func<TActor, TContext, TServiceProvider, TArg0, TArg1, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, sp, args.Get<TArg0>(0), args.Get<TArg1>(1)));
        return this;
    }
}

/// <summary>
/// Concrete builder for three-argument triggers.
/// </summary>
public sealed class StateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> :
    StateTriggerBuilderBase<TContext, TServiceProvider, TState, TActor>,
    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2>,
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2>
    where TContext : class
    where TState : struct, Enum
{
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2>.Target(TState target)
    {
        SetTarget(target);
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2>.Target(Func<TContext, TArg0, TArg1, TArg2, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, _, args) => targetSelector(context, args.Get<TArg0>(0), args.Get<TArg1>(1), args.Get<TArg2>(2)), targetHints);
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2>.Target(Func<TContext, TServiceProvider, TArg0, TArg1, TArg2, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, sp, args.Get<TArg0>(0), args.Get<TArg1>(1), args.Get<TArg2>(2)), targetHints);
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2>.Stay()
    {
        SetStay();
        return this;
    }

    void ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2>.Ignore() => SetStay();

    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2>.When(Func<TContext, TArg0, TArg1, TArg2, bool> guard, string? label)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, _, args) => guard(context, args.Get<TArg0>(0), args.Get<TArg1>(1), args.Get<TArg2>(2)), label);
        return this;
    }

    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2>.When(Func<TContext, TServiceProvider, TArg0, TArg1, TArg2, bool> guard, string? label)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, sp, args.Get<TArg0>(0), args.Get<TArg1>(1), args.Get<TArg2>(2)), label);
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2>.Invoke(Action<TContext, TArg0, TArg1, TArg2> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, _, args) => action(context, args.Get<TArg0>(0), args.Get<TArg1>(1), args.Get<TArg2>(2)));
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2>.Invoke(Action<TContext, TServiceProvider, TArg0, TArg1, TArg2> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, sp, args.Get<TArg0>(0), args.Get<TArg1>(1), args.Get<TArg2>(2)));
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2>.ReactAsync(Func<TActor, TContext, TArg0, TArg1, TArg2, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, _, args) => action(actor, context, args.Get<TArg0>(0), args.Get<TArg1>(1), args.Get<TArg2>(2)));
        return this;
    }

    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2>.ReactAsync(Func<TActor, TContext, TServiceProvider, TArg0, TArg1, TArg2, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, sp, args.Get<TArg0>(0), args.Get<TArg1>(1), args.Get<TArg2>(2)));
        return this;
    }
}

#pragma warning restore CS1591
