#pragma warning disable CS1591

namespace Nalu.SharpState;

/// <summary>
/// Shared state for the concrete <c>StateTriggerBuilder{...}</c> variants.
/// </summary>
public abstract class StateTriggerBuilderBase<TContext, TMachineArgs, TState, TActor>
    where TContext : class
    where TState : struct, Enum
{
    private TState _target;
    private Func<TContext, IServiceProvider, TMachineArgs, TState>? _targetSelector;
    private (TState Target, string Label)[]? _dynamicTargetHints;
    private bool _hasTarget;
    private bool _stay;
    private Func<TContext, IServiceProvider, TMachineArgs, bool>? _guard;
    private List<string>? _guardLabels;
    private Action<TContext, IServiceProvider, TMachineArgs>? _syncAction;
    private Func<TActor, TContext, IServiceProvider, TMachineArgs, ValueTask>? _reactionAsync;

    protected void SetTarget(TState target)
    {
        _target = target;
        _targetSelector = null;
        _dynamicTargetHints = null;
        _hasTarget = true;
    }

    protected void SetTarget(Func<TContext, IServiceProvider, TMachineArgs, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        _target = default;
        _targetSelector = targetSelector;
        _dynamicTargetHints = targetHints.Length > 0 ? targetHints : null;
        _hasTarget = true;
    }

    protected void SetStay() => _stay = true;

    protected void SetGuard(Func<TContext, IServiceProvider, TMachineArgs, bool> guard, string? label = null)
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

    protected void SetSyncAction(Action<TContext, IServiceProvider, TMachineArgs> action)
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

    protected void SetReactionAsync(Func<TActor, TContext, IServiceProvider, TMachineArgs, ValueTask> action)
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
            throw new InvalidOperationException("A transition cannot declare both TransitionTo() and Stay(). Choose one.");
        }

        if (!_hasTarget && !_stay)
        {
            throw new InvalidOperationException("A transition must declare either TransitionTo() for an external target state or Stay() for an internal transition.");
        }
    }

    public IReadOnlyList<Transition<TContext, TMachineArgs, TState, TActor>> BuildTransitions()
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
/// Builder for triggers with no method parameters (no typed trigger payload).
/// Uses the four-parameter arity while <see cref="StateTriggerBuilder{TContext, TMachineArgs, TState, TActor, TArgs}"/>
/// handles triggers with a typed payload.
/// </summary>
public sealed class StateTriggerBuilder<TContext, TMachineArgs, TState, TActor> :
    StateTriggerBuilderBase<TContext, TMachineArgs, TState, TActor>,
    IStateTriggerBuilder<TContext, TState, TActor>,
    IStateTransitionBuilder<TContext, TState, TActor>
    where TContext : class
    where TState : struct, Enum
{
    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo(TState target)
    {
        SetTarget(target);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo(StateTriggerParameterlessTargetSelector<TContext, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, _, _) => targetSelector(context), targetHints);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo<T1>(StateTriggerParameterlessTargetSelector<TContext, T1, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, _) => targetSelector(context, StateMachineServiceResolver.Resolve<T1>(sp)), targetHints);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo<T1, T2>(StateTriggerParameterlessTargetSelector<TContext, T1, T2, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, _) => targetSelector(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp)), targetHints);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo<T1, T2, T3>(StateTriggerParameterlessTargetSelector<TContext, T1, T2, T3, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, _) => targetSelector(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp)), targetHints);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo<T1, T2, T3, T4>(StateTriggerParameterlessTargetSelector<TContext, T1, T2, T3, T4, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, _) => targetSelector(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp)), targetHints);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo<T1, T2, T3, T4, T5>(StateTriggerParameterlessTargetSelector<TContext, T1, T2, T3, T4, T5, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, _) => targetSelector(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp)), targetHints);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo<T1, T2, T3, T4, T5, T6>(StateTriggerParameterlessTargetSelector<TContext, T1, T2, T3, T4, T5, T6, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, _) => targetSelector(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp)), targetHints);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo<T1, T2, T3, T4, T5, T6, T7>(StateTriggerParameterlessTargetSelector<TContext, T1, T2, T3, T4, T5, T6, T7, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, _) => targetSelector(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp)), targetHints);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8>(StateTriggerParameterlessTargetSelector<TContext, T1, T2, T3, T4, T5, T6, T7, T8, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, _) => targetSelector(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp)), targetHints);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8, T9>(StateTriggerParameterlessTargetSelector<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, _) => targetSelector(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp)), targetHints);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(StateTriggerParameterlessTargetSelector<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, _) => targetSelector(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp)), targetHints);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(StateTriggerParameterlessTargetSelector<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, _) => targetSelector(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp)), targetHints);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(StateTriggerParameterlessTargetSelector<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, _) => targetSelector(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp)), targetHints);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(StateTriggerParameterlessTargetSelector<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, _) => targetSelector(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp)), targetHints);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(StateTriggerParameterlessTargetSelector<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, _) => targetSelector(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp)), targetHints);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(StateTriggerParameterlessTargetSelector<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, _) => targetSelector(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp)), targetHints);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(StateTriggerParameterlessTargetSelector<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, _) => targetSelector(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp), StateMachineServiceResolver.Resolve<T16>(sp)), targetHints);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Stay()
    {
        SetStay();
        return this;
    }

    public void Ignore() => SetStay();

    public IStateTriggerBuilder<TContext, TState, TActor> When(StateTriggerParameterlessGuard<TContext> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, _, _) => guard(context), label);
        return this;
    }

    public IStateTriggerBuilder<TContext, TState, TActor> When<T1>(StateTriggerParameterlessGuard<TContext, T1> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, _) => guard(context, StateMachineServiceResolver.Resolve<T1>(sp)), label);
        return this;
    }

    public IStateTriggerBuilder<TContext, TState, TActor> When<T1, T2>(StateTriggerParameterlessGuard<TContext, T1, T2> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, _) => guard(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp)), label);
        return this;
    }

    public IStateTriggerBuilder<TContext, TState, TActor> When<T1, T2, T3>(StateTriggerParameterlessGuard<TContext, T1, T2, T3> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, _) => guard(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp)), label);
        return this;
    }

    public IStateTriggerBuilder<TContext, TState, TActor> When<T1, T2, T3, T4>(StateTriggerParameterlessGuard<TContext, T1, T2, T3, T4> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, _) => guard(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp)), label);
        return this;
    }

    public IStateTriggerBuilder<TContext, TState, TActor> When<T1, T2, T3, T4, T5>(StateTriggerParameterlessGuard<TContext, T1, T2, T3, T4, T5> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, _) => guard(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp)), label);
        return this;
    }

    public IStateTriggerBuilder<TContext, TState, TActor> When<T1, T2, T3, T4, T5, T6>(StateTriggerParameterlessGuard<TContext, T1, T2, T3, T4, T5, T6> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, _) => guard(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp)), label);
        return this;
    }

    public IStateTriggerBuilder<TContext, TState, TActor> When<T1, T2, T3, T4, T5, T6, T7>(StateTriggerParameterlessGuard<TContext, T1, T2, T3, T4, T5, T6, T7> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, _) => guard(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp)), label);
        return this;
    }

    public IStateTriggerBuilder<TContext, TState, TActor> When<T1, T2, T3, T4, T5, T6, T7, T8>(StateTriggerParameterlessGuard<TContext, T1, T2, T3, T4, T5, T6, T7, T8> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, _) => guard(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp)), label);
        return this;
    }

    public IStateTriggerBuilder<TContext, TState, TActor> When<T1, T2, T3, T4, T5, T6, T7, T8, T9>(StateTriggerParameterlessGuard<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, _) => guard(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp)), label);
        return this;
    }

    public IStateTriggerBuilder<TContext, TState, TActor> When<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(StateTriggerParameterlessGuard<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, _) => guard(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp)), label);
        return this;
    }

    public IStateTriggerBuilder<TContext, TState, TActor> When<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(StateTriggerParameterlessGuard<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, _) => guard(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp)), label);
        return this;
    }

    public IStateTriggerBuilder<TContext, TState, TActor> When<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(StateTriggerParameterlessGuard<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, _) => guard(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp)), label);
        return this;
    }

    public IStateTriggerBuilder<TContext, TState, TActor> When<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(StateTriggerParameterlessGuard<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, _) => guard(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp)), label);
        return this;
    }

    public IStateTriggerBuilder<TContext, TState, TActor> When<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(StateTriggerParameterlessGuard<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, _) => guard(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp)), label);
        return this;
    }

    public IStateTriggerBuilder<TContext, TState, TActor> When<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(StateTriggerParameterlessGuard<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, _) => guard(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp)), label);
        return this;
    }

    public IStateTriggerBuilder<TContext, TState, TActor> When<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(StateTriggerParameterlessGuard<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, _) => guard(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp), StateMachineServiceResolver.Resolve<T16>(sp)), label);
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Invoke(StateLifecycleAction<TContext> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, _, _) => action(context));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Invoke<T1>(StateLifecycleAction<TContext, T1> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, _) => action(context, StateMachineServiceResolver.Resolve<T1>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Invoke<T1, T2>(StateLifecycleAction<TContext, T1, T2> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, _) => action(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Invoke<T1, T2, T3>(StateLifecycleAction<TContext, T1, T2, T3> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, _) => action(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Invoke<T1, T2, T3, T4>(StateLifecycleAction<TContext, T1, T2, T3, T4> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, _) => action(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Invoke<T1, T2, T3, T4, T5>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, _) => action(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Invoke<T1, T2, T3, T4, T5, T6>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, _) => action(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Invoke<T1, T2, T3, T4, T5, T6, T7>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, _) => action(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Invoke<T1, T2, T3, T4, T5, T6, T7, T8>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, _) => action(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, _) => action(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, _) => action(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, _) => action(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, _) => action(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, _) => action(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, _) => action(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, _) => action(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, _) => action(context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp), StateMachineServiceResolver.Resolve<T16>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> ReactAsync(StateTriggerParameterlessReaction<TActor, TContext> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, _, _) => action(actor, context));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> ReactAsync<T1>(StateTriggerParameterlessReaction<TActor, TContext, T1> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, _) => action(actor, context, StateMachineServiceResolver.Resolve<T1>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> ReactAsync<T1, T2>(StateTriggerParameterlessReaction<TActor, TContext, T1, T2> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, _) => action(actor, context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> ReactAsync<T1, T2, T3>(StateTriggerParameterlessReaction<TActor, TContext, T1, T2, T3> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, _) => action(actor, context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> ReactAsync<T1, T2, T3, T4>(StateTriggerParameterlessReaction<TActor, TContext, T1, T2, T3, T4> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, _) => action(actor, context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> ReactAsync<T1, T2, T3, T4, T5>(StateTriggerParameterlessReaction<TActor, TContext, T1, T2, T3, T4, T5> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, _) => action(actor, context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> ReactAsync<T1, T2, T3, T4, T5, T6>(StateTriggerParameterlessReaction<TActor, TContext, T1, T2, T3, T4, T5, T6> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, _) => action(actor, context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> ReactAsync<T1, T2, T3, T4, T5, T6, T7>(StateTriggerParameterlessReaction<TActor, TContext, T1, T2, T3, T4, T5, T6, T7> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, _) => action(actor, context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8>(StateTriggerParameterlessReaction<TActor, TContext, T1, T2, T3, T4, T5, T6, T7, T8> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, _) => action(actor, context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9>(StateTriggerParameterlessReaction<TActor, TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, _) => action(actor, context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(StateTriggerParameterlessReaction<TActor, TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, _) => action(actor, context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(StateTriggerParameterlessReaction<TActor, TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, _) => action(actor, context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(StateTriggerParameterlessReaction<TActor, TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, _) => action(actor, context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(StateTriggerParameterlessReaction<TActor, TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, _) => action(actor, context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(StateTriggerParameterlessReaction<TActor, TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, _) => action(actor, context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(StateTriggerParameterlessReaction<TActor, TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, _) => action(actor, context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp)));
        return this;
    }

    public IStateTransitionBuilder<TContext, TState, TActor> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(StateTriggerParameterlessReaction<TActor, TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, _) => action(actor, context, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp), StateMachineServiceResolver.Resolve<T16>(sp)));
        return this;
    }
}

public sealed class StateTriggerBuilder<TContext, TMachineArgs, TState, TActor, TArgs> :
    StateTriggerBuilderBase<TContext, TMachineArgs, TState, TActor>,
    IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs>,
    IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs>
    where TContext : class
    where TState : struct, Enum
{
    private readonly Func<TMachineArgs, TArgs> _getArgs;

    public StateTriggerBuilder()
        : this(static args => (TArgs)(object)args!)
    {
    }

    public StateTriggerBuilder(Func<TMachineArgs, TArgs> getArgs)
    {
        _getArgs = getArgs ?? throw new ArgumentNullException(nameof(getArgs));
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo(TState target)
    {
        SetTarget(target);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo(StateTargetSelector<TContext, TArgs, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, _, args) => targetSelector(context, _getArgs(args)), targetHints);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo<T1>(StateTargetSelector<TContext, TArgs, T1, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp)), targetHints);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo<T1, T2>(StateTargetSelector<TContext, TArgs, T1, T2, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp)), targetHints);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo<T1, T2, T3>(StateTargetSelector<TContext, TArgs, T1, T2, T3, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp)), targetHints);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo<T1, T2, T3, T4>(StateTargetSelector<TContext, TArgs, T1, T2, T3, T4, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp)), targetHints);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo<T1, T2, T3, T4, T5>(StateTargetSelector<TContext, TArgs, T1, T2, T3, T4, T5, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp)), targetHints);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo<T1, T2, T3, T4, T5, T6>(StateTargetSelector<TContext, TArgs, T1, T2, T3, T4, T5, T6, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp)), targetHints);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo<T1, T2, T3, T4, T5, T6, T7>(StateTargetSelector<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp)), targetHints);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8>(StateTargetSelector<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp)), targetHints);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8, T9>(StateTargetSelector<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp)), targetHints);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(StateTargetSelector<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp)), targetHints);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(StateTargetSelector<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp)), targetHints);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(StateTargetSelector<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp)), targetHints);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(StateTargetSelector<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp)), targetHints);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(StateTargetSelector<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp)), targetHints);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(StateTargetSelector<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp)), targetHints);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> TransitionTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(StateTargetSelector<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TState> targetSelector, params (TState Target, string Label)[] targetHints)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);
        SetTarget((context, sp, args) => targetSelector(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp), StateMachineServiceResolver.Resolve<T16>(sp)), targetHints);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Stay()
    {
        SetStay();
        return this;
    }

    public void Ignore() => SetStay();

    public IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs> When(StateGuard<TContext, TArgs> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, _, args) => guard(context, _getArgs(args)), label);
        return this;
    }

    public IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs> When<T1>(StateGuard<TContext, TArgs, T1> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp)), label);
        return this;
    }

    public IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs> When<T1, T2>(StateGuard<TContext, TArgs, T1, T2> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp)), label);
        return this;
    }

    public IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs> When<T1, T2, T3>(StateGuard<TContext, TArgs, T1, T2, T3> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp)), label);
        return this;
    }

    public IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs> When<T1, T2, T3, T4>(StateGuard<TContext, TArgs, T1, T2, T3, T4> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp)), label);
        return this;
    }

    public IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs> When<T1, T2, T3, T4, T5>(StateGuard<TContext, TArgs, T1, T2, T3, T4, T5> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp)), label);
        return this;
    }

    public IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs> When<T1, T2, T3, T4, T5, T6>(StateGuard<TContext, TArgs, T1, T2, T3, T4, T5, T6> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp)), label);
        return this;
    }

    public IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs> When<T1, T2, T3, T4, T5, T6, T7>(StateGuard<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp)), label);
        return this;
    }

    public IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs> When<T1, T2, T3, T4, T5, T6, T7, T8>(StateGuard<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp)), label);
        return this;
    }

    public IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs> When<T1, T2, T3, T4, T5, T6, T7, T8, T9>(StateGuard<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp)), label);
        return this;
    }

    public IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs> When<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(StateGuard<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp)), label);
        return this;
    }

    public IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs> When<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(StateGuard<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp)), label);
        return this;
    }

    public IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs> When<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(StateGuard<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp)), label);
        return this;
    }

    public IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs> When<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(StateGuard<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp)), label);
        return this;
    }

    public IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs> When<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(StateGuard<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp)), label);
        return this;
    }

    public IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs> When<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(StateGuard<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp)), label);
        return this;
    }

    public IStateTriggerArgsBuilder<TContext, TState, TActor, TArgs> When<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(StateGuard<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> guard, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(guard);
        SetGuard((context, sp, args) => guard(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp), StateMachineServiceResolver.Resolve<T16>(sp)), label);
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Invoke(StateAction<TContext, TArgs> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, _, args) => action(context, _getArgs(args)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Invoke<T1>(StateAction<TContext, TArgs, T1> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Invoke<T1, T2>(StateAction<TContext, TArgs, T1, T2> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Invoke<T1, T2, T3>(StateAction<TContext, TArgs, T1, T2, T3> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Invoke<T1, T2, T3, T4>(StateAction<TContext, TArgs, T1, T2, T3, T4> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Invoke<T1, T2, T3, T4, T5>(StateAction<TContext, TArgs, T1, T2, T3, T4, T5> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Invoke<T1, T2, T3, T4, T5, T6>(StateAction<TContext, TArgs, T1, T2, T3, T4, T5, T6> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Invoke<T1, T2, T3, T4, T5, T6, T7>(StateAction<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Invoke<T1, T2, T3, T4, T5, T6, T7, T8>(StateAction<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(StateAction<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(StateAction<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(StateAction<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(StateAction<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(StateAction<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(StateAction<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(StateAction<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(StateAction<TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetSyncAction((context, sp, args) => action(context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp), StateMachineServiceResolver.Resolve<T16>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> ReactAsync(StateReaction<TActor, TContext, TArgs> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, _, args) => action(actor, context, _getArgs(args)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> ReactAsync<T1>(StateReaction<TActor, TContext, TArgs, T1> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> ReactAsync<T1, T2>(StateReaction<TActor, TContext, TArgs, T1, T2> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> ReactAsync<T1, T2, T3>(StateReaction<TActor, TContext, TArgs, T1, T2, T3> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> ReactAsync<T1, T2, T3, T4>(StateReaction<TActor, TContext, TArgs, T1, T2, T3, T4> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> ReactAsync<T1, T2, T3, T4, T5>(StateReaction<TActor, TContext, TArgs, T1, T2, T3, T4, T5> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> ReactAsync<T1, T2, T3, T4, T5, T6>(StateReaction<TActor, TContext, TArgs, T1, T2, T3, T4, T5, T6> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> ReactAsync<T1, T2, T3, T4, T5, T6, T7>(StateReaction<TActor, TContext, TArgs, T1, T2, T3, T4, T5, T6, T7> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8>(StateReaction<TActor, TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9>(StateReaction<TActor, TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(StateReaction<TActor, TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(StateReaction<TActor, TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(StateReaction<TActor, TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(StateReaction<TActor, TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(StateReaction<TActor, TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(StateReaction<TActor, TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp)));
        return this;
    }

    public IStateTransitionArgsBuilder<TContext, TState, TActor, TArgs> ReactAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(StateReaction<TActor, TContext, TArgs, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        SetReactionAsync((actor, context, sp, args) => action(actor, context, _getArgs(args), StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp), StateMachineServiceResolver.Resolve<T16>(sp)));
        return this;
    }
}

#pragma warning restore CS1591
