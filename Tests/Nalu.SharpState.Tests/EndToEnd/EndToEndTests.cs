using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nalu.SharpState.Tests.Runtime;

namespace Nalu.SharpState.Tests.EndToEnd;

public class EndToEndTests
{
    [Fact]
    public void TriggerAdvancesStateAndRunsActionWithArgs()
    {
        var ctx = new DoorContext();
        var door = DoorMachine.CreateActor(ctx, TestServiceProviders.EmptyResolver);

        door.Open("delivery");

        door.CurrentState.Should().Be(DoorMachine.State.Opened);
        ctx.OpenCount.Should().Be(1);
        ctx.LastReason.Should().Be("delivery");
    }

    [Fact]
    public void GetInitialStateReturnsGeneratedRootInitialState()
    {
        DoorMachine.GetInitialState().Should().Be(DoorMachine.State.Closed);
        NetworkMachine.GetInitialState().Should().Be(NetworkMachine.State.Idle);
    }

    [Fact]
    public void CanTriggerMethodsReflectCurrentGeneratedActorState()
    {
        var door = DoorMachine.CreateActorWithState(new DoorContext(), TestServiceProviders.EmptyResolver, DoorMachine.State.Closed);

        door.CanOpen("delivery").Should().BeTrue();
        door.CanClose().Should().BeFalse();

        door.Open("delivery");

        door.CanOpen("again").Should().BeFalse();
        door.CanClose().Should().BeTrue();
    }

    [Fact]
    public void CloseTransitionsBackAndStateChangedFires()
    {
        var door = DoorMachine.CreateActorWithState(new DoorContext(), TestServiceProviders.EmptyResolver, DoorMachine.State.Opened);
        (DoorMachine.State from, DoorMachine.State to, DoorMachine.Trigger trigger, DoorMachine.TriggerArgs args)? captured = null;
        door.StateChanged += (f, t, tr, args) => captured = (f, t, tr, args);

        door.Close();

        door.CurrentState.Should().Be(DoorMachine.State.Closed);
        captured.Should().NotBeNull();
        captured!.Value.from.Should().Be(DoorMachine.State.Opened);
        captured.Value.to.Should().Be(DoorMachine.State.Closed);
        captured.Value.trigger.Should().Be(DoorMachine.Trigger.Close);
        captured.Value.args.Should().Be(DoorMachine.TriggerArgs.ForClose());
    }

    [Fact]
    public void CreateActorWithMicrosoftDiScopeDisposesScopedServicesAfterReactAsync()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ScopedResource>();
        using var provider = serviceCollection.BuildServiceProvider();

        // Singleton AddSingletonStateMachineServiceProviderResolver() uses StateMachineStaticServiceProviderResolver (no child scope).
        // Per-ReactAsync scoped resolution requires StateMachineServiceProviderResolver over the composition root.
        var resolver = new StateMachineServiceProviderResolver(provider);
        var ctx = new ScopedDiProbeContext();
        var machine = ScopedDiProbeMachine.CreateActorWithState(ctx, resolver, ScopedDiProbeMachine.State.Idle);
        var syncContext = new MySynchronizationContext();

        RunOn(syncContext, machine.Go);

        machine.CurrentState.Should().Be(ScopedDiProbeMachine.State.Done);
        ctx.Captured.Should().BeNull();
        syncContext.Drain();
        ctx.Captured.Should().NotBeNull();
        ctx.Captured!.DisposeCallCount.Should().Be(1);
    }

    [Fact]
    public void ReactAsyncRunsAfterTriggerReturns()
    {
        var ctx = new InspectContext();
        var machine = ReactionMachine.CreateActorWithState(ctx, TestServiceProviders.EmptyResolver, ReactionMachine.State.Idle);
        var syncContext = new MySynchronizationContext();

        RunOn(syncContext, machine.Inspect);

        machine.CurrentState.Should().Be(ReactionMachine.State.Idle);
        ctx.Inspections.Should().Be(0);
        syncContext.Drain();
        machine.CurrentState.Should().Be(ReactionMachine.State.Done);
        ctx.Inspections.Should().Be(11);
    }

    [Fact]
    public void ReactAsyncTargetTransitionCommitsBeforeReaction()
    {
        var ctx = new InspectContext();
        var machine = ReactionMachine.CreateActorWithState(ctx, TestServiceProviders.EmptyResolver, ReactionMachine.State.Idle);
        var syncContext = new MySynchronizationContext();

        RunOn(syncContext, machine.Finish);

        machine.CurrentState.Should().Be(ReactionMachine.State.Done);
        ctx.Inspections.Should().Be(0);
        syncContext.Drain();
        ctx.Inspections.Should().Be(10);
    }

    [Fact]
    public void OnUnhandledOverrideCapturesUnhandledTriggers()
    {
        var door = DoorMachine.CreateActorWithState(new DoorContext(), TestServiceProviders.EmptyResolver, DoorMachine.State.Opened);
        (DoorMachine.State state, DoorMachine.Trigger trigger, DoorMachine.TriggerArgs args)? captured = null;
        door.OnUnhandled = (s, t, a) => captured = (s, t, a);

        door.Open("ignored");

        captured.Should().NotBeNull();
        captured!.Value.state.Should().Be(DoorMachine.State.Opened);
        captured.Value.trigger.Should().Be(DoorMachine.Trigger.Open);
        captured.Value.args.TryGetValue(out DoorMachine.OpenArgs args).Should().BeTrue();
        args.Reason.Should().Be("ignored");
        door.CurrentState.Should().Be(DoorMachine.State.Opened);
    }

    [Fact]
    public void HierarchicalTargetingCompositeResolvesInitialChild()
    {
        var machine = NetworkMachine.CreateActorWithState(new NetworkContext(), TestServiceProviders.EmptyResolver, NetworkMachine.State.Idle);

        machine.Connect();

        machine.CurrentState.Should().Be(NetworkMachine.State.Authenticating);
        machine.IsIn(NetworkMachine.State.Connected).Should().BeTrue();
    }

    [Fact]
    public void HierarchicalChildInheritsParentTransitions()
    {
        var machine = NetworkMachine.CreateActorWithState(new NetworkContext(), TestServiceProviders.EmptyResolver, NetworkMachine.State.Authenticated);

        machine.Disconnect();

        machine.CurrentState.Should().Be(NetworkMachine.State.Idle);
        machine.IsIn(NetworkMachine.State.Connected).Should().BeFalse();
    }

    [Fact]
    public void IsInReturnsTrueForAncestorStates()
    {
        var machine = NetworkMachine.CreateActorWithState(new NetworkContext(), TestServiceProviders.EmptyResolver, NetworkMachine.State.Authenticated);

        machine.IsIn(NetworkMachine.State.Authenticated).Should().BeTrue();
        machine.IsIn(NetworkMachine.State.Connected).Should().BeTrue();
        machine.IsIn(NetworkMachine.State.Idle).Should().BeFalse();
    }

    [Fact]
    public void InternalTransitionRunsActionWithoutChangingState()
    {
        var ctx = new NetworkContext();
        // Entering Authenticated resolves to its initial leaf (Browsing).
        var machine = NetworkMachine.CreateActorWithState(ctx, TestServiceProviders.EmptyResolver, NetworkMachine.State.Authenticated);

        machine.Message("hello");

        machine.CurrentState.Should().Be(NetworkMachine.State.Browsing);
        machine.IsIn(NetworkMachine.State.Authenticated).Should().BeTrue();
        ctx.Log.Should().Equal("hello");
    }

    [Fact]
    public void TargetingOuterCompositeLandsOnDeepestInitialLeaf()
    {
        var machine = NetworkMachine.CreateActorWithState(new NetworkContext(), TestServiceProviders.EmptyResolver, NetworkMachine.State.Idle);

        machine.Connect();

        // Connect targets Connected -> resolves to Authenticating (no deeper initial).
        machine.CurrentState.Should().Be(NetworkMachine.State.Authenticating);
        machine.IsIn(NetworkMachine.State.Connected).Should().BeTrue();
    }

    [Fact]
    public void TargetingCompositeWithNestedInitialResolvesToDeepestLeaf()
    {
        var machine = NetworkMachine.CreateActorWithState(new NetworkContext(), TestServiceProviders.EmptyResolver, NetworkMachine.State.Authenticating);

        machine.AuthOk();

        // AuthOk targets Authenticated -> which has initial Browsing.
        machine.CurrentState.Should().Be(NetworkMachine.State.Browsing);
        machine.IsIn(NetworkMachine.State.Authenticated).Should().BeTrue();
        machine.IsIn(NetworkMachine.State.Connected).Should().BeTrue();
    }

    [Fact]
    public void DeepLeafInheritsOutermostAncestorTransitions()
    {
        var machine = NetworkMachine.CreateActorWithState(new NetworkContext(), TestServiceProviders.EmptyResolver, NetworkMachine.State.Editing);

        machine.Disconnect();

        machine.CurrentState.Should().Be(NetworkMachine.State.Idle);
        machine.IsIn(NetworkMachine.State.Connected).Should().BeFalse();
    }

    [Fact]
    public void EntryAndExitHooksRunForGeneratedMachine()
    {
        var ctx = new HookContext();
        var machine = HookMachine.CreateActorWithState(ctx, TestServiceProviders.EmptyResolver, HookMachine.State.Idle);

        machine.Start();

        machine.CurrentState.Should().Be(HookMachine.State.Running);
        ctx.Log.Should().Equal("exit:Idle", "enter:Running");
    }

    [Fact]
    public void IgnoreSyntaxSugarKeepsCurrentStateWithoutUnhandled()
    {
        var ctx = new HookContext();
        var machine = HookMachine.CreateActorWithState(ctx, TestServiceProviders.EmptyResolver, HookMachine.State.Running);

        var act = machine.Ping;

        act.Should().NotThrow();
        machine.CurrentState.Should().Be(HookMachine.State.Running);
        ctx.Log.Should().BeEmpty();
    }

    private static void RunOn(MySynchronizationContext synchronizationContext, Action action)
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

    private sealed class MySynchronizationContext : SynchronizationContext
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
}
