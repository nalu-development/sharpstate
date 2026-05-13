using FluentAssertions;
using TriggerArgs = Nalu.SharpState.Tests.Runtime.TestTriggerArgs;

namespace Nalu.SharpState.Tests.Runtime;

public class AsyncTests
{
    private static readonly AsyncLocal<string?> _ambientValue = new();

    [Fact]
    public void Fire_schedules_reaction_after_transition_finishes()
    {
        var syncContext = new RecordingSynchronizationContext();
        var actor = new TestActor();
        TestActor? observedActor = null;
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .WhenExiting((ctx, _) => ctx.Log.Add("exit:A"))
            .On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                FlatState.B,
                syncAction: (ctx, _, _) => ctx.Log.Add("invoke"),
                reactionAsync: async (reactionActor, ctx, _, _) =>
                {
                    observedActor = reactionActor;
                    await Task.Yield();
                    ctx.Log.Add("react");
                }));
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .WhenEntering((ctx, _) => ctx.Log.Add("enter:B"));
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
            FlatState.A,
            ctx,
            actor,
            TestServiceProviders.EmptyResolver);
        engine.StateChanged += (_, _, _, _) => ctx.Log.Add("changed");

        RunOn(syncContext, () => engine.Fire(FlatTrigger.Go, TriggerArgs.Empty));

        ctx.Log.Should().Equal("exit:A", "invoke", "enter:B", "changed");
        syncContext.Drain();
        observedActor.Should().BeSameAs(actor);
        ctx.Log.Should().Equal("exit:A", "invoke", "enter:B", "changed", "react");
    }

    [Fact]
    public void Engine_Context_exposes_same_instance()
    {
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
            FlatState.A,
            ctx,
            new TestActor(),
            TestServiceProviders.EmptyResolver);

        engine.Context.Should().BeSameAs(ctx);
    }

    [Fact]
    public async Task Reaction_flows_AsyncLocal_from_fire_call_when_queued_to_thread_pool()
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
            using var releaseReaction = new ManualResetEventSlim();
            var observed = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var cfg = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
            cfg.On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                FlatState.B,
                reactionAsync: (_, _, _, _) =>
                {
                    if (!releaseReaction.Wait(TimeSpan.FromSeconds(2)))
                    {
                        throw new TimeoutException("Timed out waiting for the test to release the reaction.");
                    }

                    observed.SetResult(_ambientValue.Value);
                    return ValueTask.CompletedTask;
                }));
            map[FlatState.A] = cfg;
            map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
            map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

            var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
                new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
                FlatState.A,
                new TestContext(),
                new TestActor(),
                TestServiceProviders.EmptyResolver);

            _ambientValue.Value = "from-fire";
#pragma warning disable VSTHRD103 // intentional: test sync Fire from async context
            engine.Fire(FlatTrigger.Go, TriggerArgs.Empty);
#pragma warning restore VSTHRD103
            _ambientValue.Value = null;
            releaseReaction.Set();

            var value = await observed.Task.WaitAsync(TimeSpan.FromSeconds(2));
            value.Should().Be("from-fire");
        }
        finally
        {
            _ambientValue.Value = null;
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    [Fact]
    public async Task Reaction_runs_on_thread_pool_when_no_synchronization_context()
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
            var cfg = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
            var tcs = new TaskCompletionSource();
            cfg.On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                FlatState.B,
                reactionAsync: async (_, _, _, _) =>
                {
                    await Task.Yield();
                    tcs.SetResult();
                }));
            map[FlatState.A] = cfg;
            map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
            map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

            var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
                new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
                FlatState.A,
                new TestContext(),
                new TestActor(),
                TestServiceProviders.EmptyResolver);
#pragma warning disable VSTHRD103 // intentional: test sync Fire from async context
            engine.Fire(FlatTrigger.Go, TriggerArgs.Empty);
#pragma warning restore VSTHRD103
            await tcs.Task;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    [Fact]
    public void ReactionFailed_subscriber_throw_is_ignored()
    {
        var syncContext = new RecordingSynchronizationContext();
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        var cfg = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        cfg.On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(
            FlatState.B,
            reactionAsync: async (_, _, _, _) =>
            {
                await Task.Yield();
                throw new InvalidOperationException("reaction");
            }));
        map[FlatState.A] = cfg;
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
            FlatState.A,
            new TestContext(),
            new TestActor(),
            TestServiceProviders.EmptyResolver);
        engine.ReactionFailed += (_, _, _, _, _) => throw new Exception("sub");

        RunOn(syncContext, () => engine.Fire(FlatTrigger.Go, TriggerArgs.Empty));
        syncContext.Drain();
    }

    [Fact]
    public void ReactionFailed_is_raised_when_background_reaction_throws()
    {
        var syncContext = new RecordingSynchronizationContext();
        var cfg = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        cfg.On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(
            FlatState.B,
            reactionAsync: (_, _, _, _) => throw new InvalidOperationException("boom")));

        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = cfg;
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
            FlatState.A,
            new TestContext(),
            new TestActor(),
            TestServiceProviders.EmptyResolver);
        (FlatState from, FlatState to, FlatTrigger trigger, IServiceProvider args, Exception exception)? failure = null;
        engine.ReactionFailed += (from, to, trigger, args, exception) => failure = (from, to, trigger, args, exception);

        RunOn(syncContext, () => engine.Fire(FlatTrigger.Go, TriggerArgs.From(5)));
        syncContext.Drain();

        engine.CurrentState.Should().Be(FlatState.B);
        failure.Should().NotBeNull();
        failure!.Value.from.Should().Be(FlatState.A);
        failure.Value.to.Should().Be(FlatState.B);
        failure.Value.trigger.Should().Be(FlatTrigger.Go);
        failure.Value.args.Get<int>(0).Should().Be(5);
        failure.Value.exception.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Be("boom");
    }

    [Fact]
    public void Fire_schedules_post_transition_async_in_order_exitedAsync_enteredAsync_react()
    {
        var syncContext = new RecordingSynchronizationContext();
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .WhenExiting((ctx, _) => ctx.Log.Add("exit:A"))
            .WhenExitedAsync((ctx, _) =>
            {
                ctx.Log.Add("exitedAsync:A");
                return ValueTask.CompletedTask;
            })
            .On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                FlatState.B,
                syncAction: (ctx, _, _) => ctx.Log.Add("invoke"),
                reactionAsync: (_, ctx, _, _) =>
                {
                    ctx.Log.Add("react");
                    return ValueTask.CompletedTask;
                }));
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .WhenEntering((ctx, _) => ctx.Log.Add("enter:B"))
            .WhenEnteredAsync((ctx, _) =>
            {
                ctx.Log.Add("enteredAsync:B");
                return ValueTask.CompletedTask;
            });
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
            FlatState.A,
            ctx,
            new TestActor(),
            TestServiceProviders.EmptyResolver);
        engine.StateChanged += (_, _, _, _) => ctx.Log.Add("changed");

        RunOn(syncContext, () => engine.Fire(FlatTrigger.Go, TriggerArgs.Empty));

        ctx.Log.Should().Equal("exit:A", "invoke", "enter:B", "changed");
        syncContext.Drain();
        ctx.Log.Should().Equal("exit:A", "invoke", "enter:B", "changed", "exitedAsync:A", "enteredAsync:B", "react");
    }

    [Fact]
    public void Fire_scheduled_async_uses_single_scoped_service_provider_for_full_pipeline()
    {
        var syncContext = new RecordingSynchronizationContext();
        var rootProvider = EmptyServiceProvider.Instance;
        var countingResolver = new CountingScopeServiceProviderResolver(rootProvider);

        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .WhenExitedAsync((_, _) => ValueTask.CompletedTask)
            .On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                FlatState.B,
                reactionAsync: (_, _, _, _) => ValueTask.CompletedTask));
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .WhenEnteredAsync((_, _) => ValueTask.CompletedTask);
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
            FlatState.A,
            new TestContext(),
            new TestActor(),
            countingResolver);

        RunOn(syncContext, () => engine.Fire(FlatTrigger.Go, TriggerArgs.Empty));
        countingResolver.ScopeCreateCount.Should().Be(0);
        syncContext.Drain();
        countingResolver.ScopeCreateCount.Should().Be(1);
    }

    [Fact]
    public async Task FireAsync_awaits_post_transition_async_before_returning()
    {
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                FlatState.B,
                reactionAsync: async (_, ctx, _, _) =>
                {
                    await Task.Yield();
                    ctx.Log.Add("react");
                }));
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var ctx = new TestContext();
        var countingResolver = new CountingScopeServiceProviderResolver(EmptyServiceProvider.Instance);
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
            FlatState.A,
            ctx,
            new TestActor(),
            countingResolver);

        await engine.FireAsync(FlatTrigger.Go, TriggerArgs.Empty);

        ctx.Log.Should().ContainSingle().Which.Should().Be("react");
        countingResolver.ScopeCreateCount.Should().Be(0);
    }

    [Fact]
    public async Task FireAsync_wraps_WhenEnteredAsync_failure_in_ReactionFailedException()
    {
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.B));
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .WhenEnteredAsync((_, _) => throw new FormatException("entered"));
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
            FlatState.A,
            new TestContext(),
            new TestActor(),
            TestServiceProviders.EmptyResolver);
        var reactionFailed = false;
        engine.ReactionFailed += (_, _, _, _, _) => reactionFailed = true;

        var ex = await FluentActions.Awaiting(async () => await engine.FireAsync(FlatTrigger.Go, TriggerArgs.Empty))
            .Should().ThrowAsync<ReactionFailedException>();
        reactionFailed.Should().BeTrue();
        ex.Which.InnerException.Should().BeOfType<FormatException>().Which.Message.Should().Be("entered");
    }

    [Fact]
    public async Task FireAsync_wraps_WhenExitedAsync_failure_in_ReactionFailedException()
    {
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .WhenExitedAsync((_, _) => throw new FormatException("exited"))
            .On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.B));
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
            FlatState.A,
            new TestContext(),
            new TestActor(),
            TestServiceProviders.EmptyResolver);

        var ex = await FluentActions.Awaiting(async () => await engine.FireAsync(FlatTrigger.Go, TriggerArgs.Empty))
            .Should().ThrowAsync<ReactionFailedException>();
        ex.Which.InnerException.Should().BeOfType<FormatException>().Which.Message.Should().Be("exited");
    }

    [Fact]
    public async Task FireAsync_wraps_ReactAsync_failure_in_ReactionFailedException()
    {
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                FlatState.B,
                reactionAsync: (_, _, _, _) => throw new FormatException("react")));
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
            FlatState.A,
            new TestContext(),
            new TestActor(),
            TestServiceProviders.EmptyResolver);
        var ex = await FluentActions.Awaiting(async () => await engine.FireAsync(FlatTrigger.Go, TriggerArgs.Empty))
            .Should().ThrowAsync<ReactionFailedException>();
        ex.Which.InnerException.Should().BeOfType<FormatException>().Which.Message.Should().Be("react");
    }

    [Fact]
    public async Task FireAsync_guard_evaluation_exception_is_not_wrapped_in_ReactionFailedException()
    {
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                FlatState.B,
                guard: (_, _, _) => throw new FormatException("guard")));
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
            FlatState.A,
            new TestContext(),
            new TestActor(),
            TestServiceProviders.EmptyResolver);

        await FluentActions.Awaiting(async () => await engine.FireAsync(FlatTrigger.Go, TriggerArgs.Empty))
            .Should().ThrowAsync<FormatException>().Where(e => e.Message == "guard");
    }

    [Fact]
    public async Task FireAsync_sync_WhenExiting_exception_is_not_wrapped_in_ReactionFailedException()
    {
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .WhenExiting((_, _) => throw new FormatException("sync-exit"))
            .On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.B));
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
            FlatState.A,
            new TestContext(),
            new TestActor(),
            TestServiceProviders.EmptyResolver);

        await FluentActions.Awaiting(async () => await engine.FireAsync(FlatTrigger.Go, TriggerArgs.Empty))
            .Should().ThrowAsync<FormatException>().Where(e => e.Message == "sync-exit");
    }

    [Fact]
    public async Task FireAsync_ReactionFailed_subscriber_exception_is_ignored()
    {
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                FlatState.B,
                reactionAsync: async (_, _, _, _) =>
                {
                    await Task.Yield();
                    throw new InvalidOperationException("reaction");
                }));
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
            FlatState.A,
            new TestContext(),
            new TestActor(),
            TestServiceProviders.EmptyResolver);
        engine.ReactionFailed += (_, _, _, _, _) => throw new Exception("sub");

        var ex = await FluentActions.Awaiting(async () => await engine.FireAsync(FlatTrigger.Go, TriggerArgs.Empty))
            .Should().ThrowAsync<ReactionFailedException>();
        ex.Which.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void Internal_transition_runs_reaction_but_skips_async_lifecycle_hooks()
    {
        var syncContext = new RecordingSynchronizationContext();
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .WhenEnteredAsync((ctx, _) =>
            {
                ctx.Log.Add("enteredAsync");
                return ValueTask.CompletedTask;
            })
            .On(FlatTrigger.Go, TestTransition.Stay<TestContext, IServiceProvider, FlatState, TestActor>(
                reactionAsync: (_, ctx, _, _) =>
                {
                    ctx.Log.Add("react");
                    return ValueTask.CompletedTask;
                }));
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
            FlatState.B,
            ctx,
            new TestActor(),
            TestServiceProviders.EmptyResolver);

        RunOn(syncContext, () => engine.Fire(FlatTrigger.Go, TriggerArgs.Empty));
        syncContext.Drain();

        ctx.Log.Should().Equal("react");
    }

    [Fact]
    public async Task Dynamic_transition_resolved_to_current_leaf_skips_async_lifecycle_but_runs_reaction()
    {
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .WhenEnteredAsync((ctx, _) =>
            {
                ctx.Log.Add("enteredAsync");
                return ValueTask.CompletedTask;
            })
            .On(FlatTrigger.Alt, TestTransition.ToDynamicTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                (_, _, _) => FlatState.B,
                reactionAsync: async (_, ctx, _, _) =>
                {
                    await Task.Yield();
                    ctx.Log.Add("react");
                }));
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
            FlatState.B,
            ctx,
            new TestActor(),
            TestServiceProviders.EmptyResolver);

        await engine.FireAsync(FlatTrigger.Alt, TriggerArgs.Empty);

        ctx.Log.Should().Equal("react");
    }

    [Fact]
    public void External_transition_with_only_async_lifecycle_schedules_pipeline()
    {
        var syncContext = new RecordingSynchronizationContext();
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .WhenExitedAsync((ctx, _) =>
            {
                ctx.Log.Add("exitedAsync");
                return ValueTask.CompletedTask;
            })
            .On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.B));
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .WhenEnteredAsync((ctx, _) =>
            {
                ctx.Log.Add("enteredAsync");
                return ValueTask.CompletedTask;
            });
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map),
            FlatState.A,
            ctx,
            new TestActor(),
            TestServiceProviders.EmptyResolver);

        RunOn(syncContext, () => engine.Fire(FlatTrigger.Go, TriggerArgs.Empty));
        syncContext.Drain();

        ctx.Log.Should().Equal("exitedAsync", "enteredAsync");
    }

    [Fact]
    public async Task FireAsync_allows_async_trigger_from_post_transition_reaction()
    {
        var actor = new ReentrantTestActor();
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, ReentrantTestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, ReentrantTestActor>()
            .On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, ReentrantTestActor>(
                FlatState.B,
                reactionAsync: async (reactionActor, ctx, _, _) =>
                {
                    ctx.Log.Add("react:go");
                    await reactionActor.Engine!.FireAsync(FlatTrigger.Alt, TriggerArgs.Empty);
                }));
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, ReentrantTestActor>()
            .On(FlatTrigger.Alt, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, ReentrantTestActor>(
                FlatState.C,
                reactionAsync: (_, ctx, _, _) =>
                {
                    ctx.Log.Add("react:alt");
                    return ValueTask.CompletedTask;
                }));
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, ReentrantTestActor>();

        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, ReentrantTestActor>(
            new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, ReentrantTestActor>(map),
            FlatState.A,
            ctx,
            actor,
            TestServiceProviders.EmptyResolver);
        actor.Engine = engine;

        await engine.FireAsync(FlatTrigger.Go, TriggerArgs.Empty);

        engine.CurrentState.Should().Be(FlatState.C);
        ctx.Log.Should().Equal("react:go", "react:alt");
    }

    [Fact]
    public void Fire_unhandled_trigger_invokes_callback()
    {
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var definition = new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map);
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(definition, FlatState.A, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);
        (FlatState state, FlatTrigger trigger, IServiceProvider args)? captured = null;
        engine.OnUnhandled = (state, trigger, args) => captured = (state, trigger, args);

        engine.Fire(FlatTrigger.NoMatch, TriggerArgs.From(5));

        captured.Should().NotBeNull();
        captured!.Value.state.Should().Be(FlatState.A);
        captured.Value.trigger.Should().Be(FlatTrigger.NoMatch);
        captured.Value.args.Get<int>(0).Should().Be(5);
    }

    [Fact]
    public void Fire_unhandled_with_default_callback_throws()
    {
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var definition = new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map);
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(definition, FlatState.A, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);

        var act = () => engine.Fire(FlatTrigger.NoMatch, TriggerArgs.Empty);

        act.Should().Throw<InvalidOperationException>();
    }

    private static void RunOn(RecordingSynchronizationContext synchronizationContext, Action action)
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        try
        {
            action();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private sealed class RecordingSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State)> _queue = new();

        public override void Post(SendOrPostCallback d, object? state) => _queue.Enqueue((d, state));

        public void Drain()
        {
            while (_queue.Count > 0)
            {
                var (callback, state) = _queue.Dequeue();
                var previous = Current;
                SetSynchronizationContext(this);
                try
                {
                    callback(state);
                }
                finally
                {
                    SetSynchronizationContext(previous);
                }
            }
        }
    }

    private sealed class ReentrantTestActor
    {
        public StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, ReentrantTestActor>? Engine { get; set; }
    }
}
