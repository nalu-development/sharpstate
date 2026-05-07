#pragma warning disable CS1591

namespace Nalu.SharpState;

/// <summary>
/// Continuation builder for a configured transition with no trigger parameters.
/// </summary>
public interface ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor>
    where TContext : class
    where TState : struct, Enum
{
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor> Invoke(Action<TContext> action);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor> Invoke(Action<TContext, TServiceProvider> action);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor> ReactAsync(Func<TActor, TContext, ValueTask> action);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor> ReactAsync(Func<TActor, TContext, TServiceProvider, ValueTask> action);
}

/// <summary>
/// Fluent builder for triggers that take no parameters.
/// </summary>
public interface ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor>
    where TContext : class
    where TState : struct, Enum
{
    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor> When(Func<TContext, bool> guard, string? label = null);
    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor> When(Func<TContext, TServiceProvider, bool> guard, string? label = null);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor> Target(TState target);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor> Target(Func<TContext, TState> targetSelector, params (TState Target, string Label)[] targetHints);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor> Target(Func<TContext, TServiceProvider, TState> targetSelector, params (TState Target, string Label)[] targetHints);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor> Stay();
    void Ignore();
}

/// <summary>
/// Continuation builder for a configured transition with one trigger parameter.
/// </summary>
public interface ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0>
    where TContext : class
    where TState : struct, Enum
{
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0> Invoke(Action<TContext, TArg0> action);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0> Invoke(Action<TContext, TServiceProvider, TArg0> action);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0> ReactAsync(Func<TActor, TContext, TArg0, ValueTask> action);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0> ReactAsync(Func<TActor, TContext, TServiceProvider, TArg0, ValueTask> action);
}

/// <summary>
/// Fluent builder for triggers that take one parameter.
/// </summary>
public interface ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0>
    where TContext : class
    where TState : struct, Enum
{
    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0> When(Func<TContext, TArg0, bool> guard, string? label = null);
    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0> When(Func<TContext, TServiceProvider, TArg0, bool> guard, string? label = null);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0> Target(TState target);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0> Target(Func<TContext, TArg0, TState> targetSelector, params (TState Target, string Label)[] targetHints);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0> Target(Func<TContext, TServiceProvider, TArg0, TState> targetSelector, params (TState Target, string Label)[] targetHints);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0> Stay();
    void Ignore();
}

/// <summary>
/// Continuation builder for a configured transition with two trigger parameters.
/// </summary>
public interface ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1>
    where TContext : class
    where TState : struct, Enum
{
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> Invoke(Action<TContext, TArg0, TArg1> action);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> Invoke(Action<TContext, TServiceProvider, TArg0, TArg1> action);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> ReactAsync(Func<TActor, TContext, TArg0, TArg1, ValueTask> action);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> ReactAsync(Func<TActor, TContext, TServiceProvider, TArg0, TArg1, ValueTask> action);
}

/// <summary>
/// Fluent builder for triggers that take two parameters.
/// </summary>
public interface ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1>
    where TContext : class
    where TState : struct, Enum
{
    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> When(Func<TContext, TArg0, TArg1, bool> guard, string? label = null);
    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> When(Func<TContext, TServiceProvider, TArg0, TArg1, bool> guard, string? label = null);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> Target(TState target);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> Target(Func<TContext, TArg0, TArg1, TState> targetSelector, params (TState Target, string Label)[] targetHints);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> Target(Func<TContext, TServiceProvider, TArg0, TArg1, TState> targetSelector, params (TState Target, string Label)[] targetHints);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1> Stay();
    void Ignore();
}

/// <summary>
/// Continuation builder for a configured transition with three trigger parameters.
/// </summary>
public interface ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2>
    where TContext : class
    where TState : struct, Enum
{
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> Invoke(Action<TContext, TArg0, TArg1, TArg2> action);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> Invoke(Action<TContext, TServiceProvider, TArg0, TArg1, TArg2> action);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> ReactAsync(Func<TActor, TContext, TArg0, TArg1, TArg2, ValueTask> action);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> ReactAsync(Func<TActor, TContext, TServiceProvider, TArg0, TArg1, TArg2, ValueTask> action);
}

/// <summary>
/// Fluent builder for triggers that take three parameters.
/// </summary>
public interface ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2>
    where TContext : class
    where TState : struct, Enum
{
    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> When(Func<TContext, TArg0, TArg1, TArg2, bool> guard, string? label = null);
    ISyncStateTriggerBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> When(Func<TContext, TServiceProvider, TArg0, TArg1, TArg2, bool> guard, string? label = null);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> Target(TState target);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> Target(Func<TContext, TArg0, TArg1, TArg2, TState> targetSelector, params (TState Target, string Label)[] targetHints);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> Target(Func<TContext, TServiceProvider, TArg0, TArg1, TArg2, TState> targetSelector, params (TState Target, string Label)[] targetHints);
    ISyncStateTransitionBuilder<TContext, TServiceProvider, TState, TActor, TArg0, TArg1, TArg2> Stay();
    void Ignore();
}

#pragma warning restore CS1591
