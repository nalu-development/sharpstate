namespace Nalu.SharpState;

/// <summary>
/// Runtime dispatcher used by every generated actor: holds the current leaf state and the caller's context,
/// walks the hierarchy to resolve transitions, commits state changes, and schedules post-transition asynchronous work.
/// </summary>
/// <typeparam name="TContext">Type of the user-supplied context carried by the machine.</typeparam>
/// <typeparam name="TArgs">Machine-specific trigger argument union.</typeparam>
/// <typeparam name="TState">Enum type listing all states of the machine.</typeparam>
/// <typeparam name="TTrigger">Enum type listing all triggers of the machine.</typeparam>
/// <typeparam name="TActor">Type of the actor passed into post-transition asynchronous work.</typeparam>
public sealed class StateMachineEngine<TContext, TArgs, TState, TTrigger, TActor>
    where TContext : class
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    private readonly StateMachineDefinition<TContext, TArgs, TState, TTrigger, TActor> _definition;
    private readonly TActor _actor;
    private readonly TContext _context;
    private readonly IStateMachineServiceProviderResolver _serviceProviderResolver;
    private readonly IServiceProvider _serviceProvider;
    private TState _currentState;
    private bool _isDispatching;

    /// <summary>
    /// Initializes a new <see cref="StateMachineEngine{TContext, TArgs, TState, TTrigger, TActor}"/>.
    /// If <paramref name="currentState"/> is a composite, its initial child chain is resolved to a leaf before the engine settles.
    /// </summary>
    /// <param name="definition">The immutable definition to dispatch against.</param>
    /// <param name="currentState">The initial state. Composites are resolved to their initial leaf.</param>
    /// <param name="context">The context carried through every transition.</param>
    /// <param name="actor">The actor instance to pass to post-transition asynchronous work.</param>
    /// <param name="serviceProviderResolver">Resolver whose provider is captured for synchronous dispatch; scheduled post-transition work uses <see cref="IStateMachineServiceProviderResolver.CreateScopedServiceProvider"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="definition"/>, <paramref name="context"/>, or <paramref name="serviceProviderResolver"/> is <c>null</c>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="currentState"/> is not registered in the definition.</exception>
    public StateMachineEngine(
        StateMachineDefinition<TContext, TArgs, TState, TTrigger, TActor> definition,
        TState currentState,
        TContext context,
        TActor actor,
        IStateMachineServiceProviderResolver serviceProviderResolver)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _actor = actor;
        _serviceProviderResolver = serviceProviderResolver ?? throw new ArgumentNullException(nameof(serviceProviderResolver));
        _serviceProvider = _serviceProviderResolver.GetServiceProvider();

        if (!_definition.TryGetConfiguration(currentState, out _))
        {
            throw new KeyNotFoundException($"State '{currentState}' is not registered in the state machine definition.");
        }

        _currentState = _definition.LeafOf(currentState);
    }

    /// <summary>
    /// The current leaf state.
    /// </summary>
    public TState CurrentState => _currentState;

    /// <summary>
    /// The user-supplied context passed to every guard and action.
    /// </summary>
    public TContext Context => _context;

    /// <summary>
    /// Raised after a transition has committed. Parameters are the source leaf, the new leaf, and the trigger that caused the change.
    /// Not raised for internal transitions (<see cref="Transition{TContext, TArgs, TState, TActor}.IsInternal"/>) or unhandled triggers.
    /// </summary>
    public event StateChangedHandler<TState, TTrigger, TArgs>? StateChanged;

    /// <summary>
    /// Raised when post-transition asynchronous work fails after the transition already completed
    /// (<c>WhenExitedAsync</c>, <c>WhenEnteredAsync</c>, <c>ReactAsync</c>).
    /// </summary>
    public event ReactionFailedHandler<TState, TTrigger, TArgs>? ReactionFailed;

    /// <summary>
    /// Callback invoked when a trigger fires but no transition matches (neither on the current leaf
    /// nor on any of its ancestors, or all guards returned <c>false</c>). Handlers receive the current
    /// leaf state, the trigger itself, and the arguments originally passed to it.
    /// Defaults to a handler that throws <see cref="InvalidOperationException"/>.
    /// Set to <c>null</c> to silently ignore unhandled triggers, or assign a custom handler.
    /// </summary>
    public UnhandledTriggerHandler<TState, TTrigger, TArgs>? OnUnhandled { get; set; } = DefaultUnhandled;

    private static void DefaultUnhandled(TState currentState, TTrigger trigger, TArgs args)
        => throw new InvalidOperationException(
            $"Trigger '{trigger}' is not handled from state '{currentState}'.");

    /// <summary>
    /// Determines whether the current leaf equals <paramref name="state"/> or any of its ancestors equals <paramref name="state"/>.
    /// </summary>
    /// <param name="state">The state to test against. May be a leaf or a composite.</param>
    /// <returns><c>true</c> if the machine is currently in <paramref name="state"/> or any of its descendants.</returns>
    public bool IsIn(TState state) => _definition.IsSelfOrDescendantOf(_currentState, state);

    /// <summary>
    /// Fires a trigger synchronously. The first matching transition wins.
    /// External transitions run exit actions, transition action, state commit, entry actions, and <see cref="StateChanged"/>
    /// before any post-transition asynchronous work is scheduled in the captured synchronization context.
    /// </summary>
    public void Fire(TTrigger trigger, TArgs args)
    {
        if (_isDispatching)
        {
            throw new InvalidOperationException(
                $"Trigger '{trigger}' cannot be fired while another trigger is still being processed. Use {nameof(FireAsync)}(...) or post-transition work instead.");
        }

        _isDispatching = true;
        TransitionPlan plan;
        try
        {
            var match = FindMatchingTransition(trigger, args);
            if (match is null)
            {
                OnUnhandled?.Invoke(_currentState, trigger, args);
                return;
            }

            var transition = match.Value.Transition;
            plan = CommitTransitionCore(trigger, match.Value.Source, transition, args, _serviceProvider);
        }
        finally
        {
            _isDispatching = false;
        }

        if (HasPostTransitionAsyncWork(plan))
        {
            SchedulePostTransitionAsync(plan);
        }
    }

    /// <summary>
    /// Fires a trigger synchronously and awaits post-transition asynchronous work (<c>WhenExitedAsync</c>, <c>WhenEnteredAsync</c>, <c>ReactAsync</c>) inline.
    /// Uses the engine-captured service provider (no reaction scope). Failures after commit raise <see cref="ReactionFailed"/> and throw <see cref="ReactionFailedException"/>.
    /// </summary>
    public async ValueTask FireAsync(TTrigger trigger, TArgs args)
    {
        if (_isDispatching)
        {
            throw new InvalidOperationException(
                $"Trigger '{trigger}' cannot be fired while another trigger is still being processed. Use post-transition work or avoid reentrancy.");
        }

        TransitionPlan plan;
        _isDispatching = true;
        try
        {
            var match = FindMatchingTransition(trigger, args);
            if (match is null)
            {
                OnUnhandled?.Invoke(_currentState, trigger, args);
                return;
            }

            var transition = match.Value.Transition;
            plan = CommitTransitionCore(trigger, match.Value.Source, transition, args, _serviceProvider);
        }
        finally
        {
            _isDispatching = false;
        }

        if (!HasPostTransitionAsyncWork(plan))
        {
            return;
        }

        try
        {
            await ExecutePostTransitionAsync(plan, _serviceProvider);
        }
        catch (Exception exception)
        {
            RaiseReactionFailedSafe(plan, exception);
            throw new ReactionFailedException(plan.Source, plan.Destination, plan.Trigger, plan.Args, exception);
        }
    }

    /// <summary>
    /// Determines whether the specified trigger currently has a matching transition.
    /// Guards are evaluated against the current state and supplied arguments, but no actions run and no state changes are committed.
    /// </summary>
    public bool CanFire(TTrigger trigger, TArgs args)
        => FindMatchingTransition(trigger, args).HasValue;

    private readonly record struct TransitionPlan(
        TState Source,
        TState Destination,
        TTrigger Trigger,
        TArgs Args,
        Transition<TContext, TArgs, TState, TActor> Transition,
        TransitionPlan.LifecyclePath? Lifecycle,
        bool HasExitedAsyncAction,
        bool HasEnteredAsyncAction)
    {
        public bool IncludeAsyncLifecycleHooks => Lifecycle is not null;

        public readonly record struct LifecyclePath(
            IReadOnlyList<TState> SourceAncestors,
            int ExitLcaIndex,
            IReadOnlyList<TState> DestinationAncestors,
            int EntryLcaIndex);
    }

    private (Transition<TContext, TArgs, TState, TActor> Transition, TState Source)? FindMatchingTransition(
        TTrigger trigger,
        TArgs args)
    {
        var source = _currentState;
        var state = source;
        while (true)
        {
            if (_definition.TryGetConfiguration(state, out var config)
                && config.TryGetTransitions(trigger, out var transitions))
            {
                foreach (var transition in transitions)
                {
                    if (transition.Guard is null || transition.Guard(_context, _serviceProvider, args))
                    {
                        return (transition, source);
                    }
                }
            }

            if (!_definition.Parent.TryGetValue(state, out var parent))
            {
                return null;
            }

            state = parent;
        }
    }

    private TransitionPlan CommitTransitionCore(
        TTrigger trigger,
        TState source,
        Transition<TContext, TArgs, TState, TActor> transition,
        TArgs args,
        IServiceProvider serviceProvider)
    {
        if (transition.TargetSelector is not null)
        {
            var resolvedTarget = transition.TargetSelector(_context, serviceProvider, args);
            var resolvedLeaf = _definition.LeafOf(resolvedTarget);
            if (EqualityComparer<TState>.Default.Equals(resolvedLeaf, source))
            {
                transition.SyncAction?.Invoke(_context, serviceProvider, args);
                return CreateInternalTransitionPlan(source, source, trigger, args, transition);
            }

            var lifecyclePath = CreateLifecyclePath(source, resolvedLeaf);
            var hasExitedAsyncAction = InvokeExitActions(source, lifecyclePath, serviceProvider);
            transition.SyncAction?.Invoke(_context, serviceProvider, args);
            _currentState = resolvedLeaf;
            var hasEnteredAsyncAction = InvokeEntryActions(resolvedLeaf, lifecyclePath, serviceProvider);
            NotifyContextAboutStateChange(resolvedLeaf);
            StateChanged?.Invoke(source, resolvedLeaf, trigger, args);
            return CreateTransitionPlan(source, resolvedLeaf, trigger, args, transition, lifecyclePath, hasExitedAsyncAction, hasEnteredAsyncAction);
        }

        if (transition.IsInternal)
        {
            transition.SyncAction?.Invoke(_context, serviceProvider, args);
            return CreateInternalTransitionPlan(source, source, trigger, args, transition);
        }

        var newLeaf = _definition.LeafOf(transition.Target);
        var staticLifecyclePath = CreateLifecyclePath(source, newLeaf);
        var hasStaticExitedAsyncAction = InvokeExitActions(source, staticLifecyclePath, serviceProvider);
        transition.SyncAction?.Invoke(_context, serviceProvider, args);
        _currentState = newLeaf;
        var hasStaticEnteredAsyncAction = InvokeEntryActions(newLeaf, staticLifecyclePath, serviceProvider);
        NotifyContextAboutStateChange(newLeaf);
        StateChanged?.Invoke(source, newLeaf, trigger, args);
        return CreateTransitionPlan(source, newLeaf, trigger, args, transition, staticLifecyclePath, hasStaticExitedAsyncAction, hasStaticEnteredAsyncAction);
    }

    private void NotifyContextAboutStateChange(TState resolvedLeaf) => (_context as IStateAwareContext<TState>)?.OnStateChanged(resolvedLeaf);

    private static TransitionPlan CreateInternalTransitionPlan(
        TState source,
        TState destination,
        TTrigger trigger,
        TArgs args,
        Transition<TContext, TArgs, TState, TActor> transition)
        => new(
            source,
            destination,
            trigger,
            args,
            transition,
            Lifecycle: null,
            HasExitedAsyncAction: false,
            HasEnteredAsyncAction: false);

    private static TransitionPlan CreateTransitionPlan(
        TState source,
        TState destination,
        TTrigger trigger,
        TArgs args,
        Transition<TContext, TArgs, TState, TActor> transition,
        TransitionPlan.LifecyclePath lifecyclePath,
        bool hasExitedAsyncAction,
        bool hasEnteredAsyncAction)
        => new(
            source,
            destination,
            trigger,
            args,
            transition,
            Lifecycle: lifecyclePath,
            hasExitedAsyncAction,
            hasEnteredAsyncAction);

    private TransitionPlan.LifecyclePath CreateLifecyclePath(TState source, TState destination)
    {
        var lca = _definition.LowestCommonAncestor(source, destination);
        var sourceAncestors = _definition.AncestorsOf(source);
        var destinationAncestors = _definition.AncestorsOf(destination);
        var exitLcaIndex = lca.HasValue ? IndexOf(sourceAncestors, lca.Value) : sourceAncestors.Count;
        var entryLcaIndex = lca.HasValue ? IndexOf(destinationAncestors, lca.Value) : -1;

        return new TransitionPlan.LifecyclePath(sourceAncestors, exitLcaIndex, destinationAncestors, entryLcaIndex);
    }

    private bool InvokeExitActions(TState source, TransitionPlan.LifecyclePath lifecyclePath, IServiceProvider serviceProvider)
    {
        var config = _definition.GetConfiguration(source);
        var hasExitedAsyncAction = config.ExitedAsyncAction is not null;
        config.ExitAction?.Invoke(_context, serviceProvider);

        for (var i = 0; i < lifecyclePath.ExitLcaIndex; i++)
        {
            config = _definition.GetConfiguration(lifecyclePath.SourceAncestors[i]);
            hasExitedAsyncAction |= config.ExitedAsyncAction is not null;
            config.ExitAction?.Invoke(_context, serviceProvider);
        }

        return hasExitedAsyncAction;
    }

    private bool InvokeEntryActions(TState destination, TransitionPlan.LifecyclePath lifecyclePath, IServiceProvider serviceProvider)
    {
        var hasEnteredAsyncAction = false;
        var destinationAncestors = lifecyclePath.DestinationAncestors;
        for (var i = destinationAncestors.Count - 1; i > lifecyclePath.EntryLcaIndex; i--)
        {
            var config = _definition.GetConfiguration(destinationAncestors[i]);
            hasEnteredAsyncAction |= config.EnteredAsyncAction is not null;
            config.EntryAction?.Invoke(_context, serviceProvider);
        }

        var destinationConfig = _definition.GetConfiguration(destination);
        hasEnteredAsyncAction |= destinationConfig.EnteredAsyncAction is not null;
        destinationConfig.EntryAction?.Invoke(_context, serviceProvider);
        return hasEnteredAsyncAction;
    }

    private bool HasPostTransitionAsyncWork(TransitionPlan plan)
        => plan.Transition.ReactionAsync is not null
            || plan.HasExitedAsyncAction
            || plan.HasEnteredAsyncAction;

    private void SchedulePostTransitionAsync(TransitionPlan plan)
    {
        var synchronizationContext = SynchronizationContext.Current;
        var workItem = new PostTransitionWorkItem(this, plan);
        if (synchronizationContext is null)
        {
            _ = Task.Run(workItem.Start);
            return;
        }

#pragma warning disable VSTHRD001
        synchronizationContext.Post(static state => ((PostTransitionWorkItem)state!).Start(), workItem);
#pragma warning restore VSTHRD001
    }

#pragma warning disable VSTHRD100
    private async void ExecutePostTransitionScheduled(TransitionPlan plan)
#pragma warning restore VSTHRD100
    {
        try
        {
            using var scopeOwnership = _serviceProviderResolver.CreateScopedServiceProvider(out var scoped);
            await ExecutePostTransitionAsync(plan, scoped);
        }
        catch (Exception exception)
        {
            RaiseReactionFailedSafe(plan, exception);
        }
    }

    private async ValueTask ExecutePostTransitionAsync(TransitionPlan plan, IServiceProvider serviceProvider)
    {
        if (plan.IncludeAsyncLifecycleHooks)
        {
            var lifecyclePath = plan.Lifecycle.GetValueOrDefault();
            var config = _definition.GetConfiguration(plan.Source);
            if (config.ExitedAsyncAction is { } exitedAsync)
            {
                await exitedAsync(_context, serviceProvider);
            }

            var sourceAncestors = lifecyclePath.SourceAncestors;
            for (var i = 0; i < lifecyclePath.ExitLcaIndex; i++)
            {
                config = _definition.GetConfiguration(sourceAncestors[i]);
                if (config.ExitedAsyncAction is { } ancestorExitedAsync)
                {
                    await ancestorExitedAsync(_context, serviceProvider);
                }
            }
        }

        if (plan.IncludeAsyncLifecycleHooks)
        {
            var lifecyclePath = plan.Lifecycle.GetValueOrDefault();
            var destinationAncestors = lifecyclePath.DestinationAncestors;
            for (var i = destinationAncestors.Count - 1; i > lifecyclePath.EntryLcaIndex; i--)
            {
                var config = _definition.GetConfiguration(destinationAncestors[i]);
                if (config.EnteredAsyncAction is { } enteredAsync)
                {
                    await enteredAsync(_context, serviceProvider);
                }
            }

            var destinationConfig = _definition.GetConfiguration(plan.Destination);
            if (destinationConfig.EnteredAsyncAction is { } destinationEnteredAsync)
            {
                await destinationEnteredAsync(_context, serviceProvider);
            }
        }

        if (plan.Transition.ReactionAsync is { } reactionAsync)
        {
            await reactionAsync(_actor, _context, serviceProvider, plan.Args);
        }
    }

    private void RaiseReactionFailedSafe(TransitionPlan plan, Exception exception)
    {
        try
        {
            ReactionFailed?.Invoke(plan.Source, plan.Destination, plan.Trigger, plan.Args, exception);
        }
        catch
        {
            // Ignore failures in failure-reporting subscribers.
        }
    }

    private static int IndexOf(IReadOnlyList<TState> states, TState state)
    {
        var comparer = EqualityComparer<TState>.Default;
        for (var i = 0; i < states.Count; i++)
        {
            if (comparer.Equals(states[i], state))
            {
                return i;
            }
        }

        return -1;
    }

    private sealed class PostTransitionWorkItem
    {
        private readonly StateMachineEngine<TContext, TArgs, TState, TTrigger, TActor> _engine;
        private readonly TransitionPlan _plan;

        public PostTransitionWorkItem(
            StateMachineEngine<TContext, TArgs, TState, TTrigger, TActor> engine,
            TransitionPlan plan)
        {
            _engine = engine;
            _plan = plan;
        }

        public void Start() => _engine.ExecutePostTransitionScheduled(_plan);
    }
}
