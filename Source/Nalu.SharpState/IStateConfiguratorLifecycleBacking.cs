namespace Nalu.SharpState;

/// <summary>
/// Mutation surface consumed by default <see cref="IStateLifecycleFluent{TFluent, TContext}"/> implementations while configuring a state,
/// aligning with generated <see cref="StateConfigurator{TContext, TArgs, TState, TTrigger, TActor}"/>.
/// Each hook may only be configured once per state.
/// </summary>
/// <typeparam name="TContext">The machine context type.</typeparam>
public interface IStateConfiguratorLifecycleBacking<TContext>
{
    /// <summary>
    /// Registers the entry callback (at most once per configurator).
    /// </summary>
    /// <param name="action">Context and synchronous transition services.</param>
    void LifecycleSetEntryAction(Action<TContext, IServiceProvider> action);

    /// <summary>
    /// Registers the exit callback (at most once per configurator).
    /// </summary>
    /// <param name="action">Context and synchronous transition services.</param>
    void LifecycleSetExitAction(Action<TContext, IServiceProvider> action);

    /// <summary>
    /// Registers the post-commit entered async callback (at most once per configurator).
    /// </summary>
    void LifecycleSetEnteredAsyncAction(Func<TContext, IServiceProvider, ValueTask> action);

    /// <summary>
    /// Registers the post-commit exited async callback (at most once per configurator).
    /// </summary>
    void LifecycleSetExitedAsyncAction(Func<TContext, IServiceProvider, ValueTask> action);
}
