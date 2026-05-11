// ReSharper disable SuspiciousTypeConversion.Global
namespace Nalu.SharpState;

/// <summary>
/// Fluent entry/exit hooks for state configuration. Default implementations rely on casting to
/// <see cref="IStateConfiguratorLifecycleBacking{TContext}"/> (provided by inheriting from
/// <see cref="StateConfigurator{TContext, TArgs, TState, TTrigger, TActor}"/>).
/// </summary>
/// <typeparam name="TFluent">The concrete fluent type (curiously recurring pattern).</typeparam>
/// <typeparam name="TContext">The machine context type.</typeparam>
public interface IStateLifecycleFluent<out TFluent, out TContext>
    where TFluent : class, IStateLifecycleFluent<TFluent, TContext>
{

    /// <summary>
    /// Declares a synchronous callback to run after the machine enters this state.
    /// Convenience overload: the callback receives only <typeparamref name="TContext"/>.
    /// </summary>
    /// <param name="action">The callback to run after the state is entered; receives only the context.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenEntering(StateLifecycleAction<TContext> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetEntryAction((ctx, _) => action(ctx));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run after the machine enters this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run after the state is entered; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenEntering<T1>(StateLifecycleAction<TContext, T1> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetEntryAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run after the machine enters this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run after the state is entered; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenEntering<T1, T2>(StateLifecycleAction<TContext, T1, T2> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetEntryAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run after the machine enters this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run after the state is entered; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenEntering<T1, T2, T3>(StateLifecycleAction<TContext, T1, T2, T3> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetEntryAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run after the machine enters this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run after the state is entered; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenEntering<T1, T2, T3, T4>(StateLifecycleAction<TContext, T1, T2, T3, T4> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetEntryAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run after the machine enters this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run after the state is entered; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenEntering<T1, T2, T3, T4, T5>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetEntryAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run after the machine enters this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run after the state is entered; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenEntering<T1, T2, T3, T4, T5, T6>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetEntryAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run after the machine enters this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run after the state is entered; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenEntering<T1, T2, T3, T4, T5, T6, T7>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetEntryAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run after the machine enters this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run after the state is entered; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenEntering<T1, T2, T3, T4, T5, T6, T7, T8>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetEntryAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run after the machine enters this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run after the state is entered; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenEntering<T1, T2, T3, T4, T5, T6, T7, T8, T9>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetEntryAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run after the machine enters this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run after the state is entered; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenEntering<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetEntryAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run after the machine enters this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run after the state is entered; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenEntering<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetEntryAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run after the machine enters this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run after the state is entered; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenEntering<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetEntryAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run after the machine enters this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run after the state is entered; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenEntering<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetEntryAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run after the machine enters this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run after the state is entered; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenEntering<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetEntryAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run after the machine enters this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run after the state is entered; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenEntering<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetEntryAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run after the machine enters this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run after the state is entered; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenEntering<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetEntryAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp), StateMachineServiceResolver.Resolve<T16>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run before the machine exits this state.
    /// Convenience overload: the callback receives only <typeparamref name="TContext"/>.
    /// </summary>
    /// <param name="action">The callback to run before this state is left; receives only the context.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenExiting(StateLifecycleAction<TContext> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetExitAction((ctx, _) => action(ctx));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run before the machine exits this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run before this state is left; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenExiting<T1>(StateLifecycleAction<TContext, T1> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetExitAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run before the machine exits this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run before this state is left; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenExiting<T1, T2>(StateLifecycleAction<TContext, T1, T2> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetExitAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run before the machine exits this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run before this state is left; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenExiting<T1, T2, T3>(StateLifecycleAction<TContext, T1, T2, T3> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetExitAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run before the machine exits this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run before this state is left; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenExiting<T1, T2, T3, T4>(StateLifecycleAction<TContext, T1, T2, T3, T4> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetExitAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run before the machine exits this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run before this state is left; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenExiting<T1, T2, T3, T4, T5>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetExitAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run before the machine exits this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run before this state is left; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenExiting<T1, T2, T3, T4, T5, T6>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetExitAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run before the machine exits this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run before this state is left; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenExiting<T1, T2, T3, T4, T5, T6, T7>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetExitAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run before the machine exits this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run before this state is left; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenExiting<T1, T2, T3, T4, T5, T6, T7, T8>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetExitAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run before the machine exits this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run before this state is left; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenExiting<T1, T2, T3, T4, T5, T6, T7, T8, T9>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetExitAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run before the machine exits this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run before this state is left; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenExiting<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetExitAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run before the machine exits this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run before this state is left; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenExiting<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetExitAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run before the machine exits this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run before this state is left; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenExiting<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetExitAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run before the machine exits this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run before this state is left; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenExiting<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetExitAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run before the machine exits this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run before this state is left; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenExiting<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetExitAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run before the machine exits this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run before this state is left; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenExiting<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetExitAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp)));
        return (TFluent)this;
    }


    /// <summary>
    /// Declares a synchronous callback to run before the machine exits this state.
    /// Service arguments are resolved from the current <see cref="global::System.IServiceProvider"/>.
    /// </summary>
    /// <param name="action">The callback to run before this state is left; receives the context plus injected services.</param>
    /// <returns>The same configurator instance for chaining.</returns>
    TFluent WhenExiting<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(StateLifecycleAction<TContext, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var backing = (IStateConfiguratorLifecycleBacking<TContext>)this;
        backing.LifecycleSetExitAction((ctx, sp) => action(ctx, StateMachineServiceResolver.Resolve<T1>(sp), StateMachineServiceResolver.Resolve<T2>(sp), StateMachineServiceResolver.Resolve<T3>(sp), StateMachineServiceResolver.Resolve<T4>(sp), StateMachineServiceResolver.Resolve<T5>(sp), StateMachineServiceResolver.Resolve<T6>(sp), StateMachineServiceResolver.Resolve<T7>(sp), StateMachineServiceResolver.Resolve<T8>(sp), StateMachineServiceResolver.Resolve<T9>(sp), StateMachineServiceResolver.Resolve<T10>(sp), StateMachineServiceResolver.Resolve<T11>(sp), StateMachineServiceResolver.Resolve<T12>(sp), StateMachineServiceResolver.Resolve<T13>(sp), StateMachineServiceResolver.Resolve<T14>(sp), StateMachineServiceResolver.Resolve<T15>(sp), StateMachineServiceResolver.Resolve<T16>(sp)));
        return (TFluent)this;
    }
}
