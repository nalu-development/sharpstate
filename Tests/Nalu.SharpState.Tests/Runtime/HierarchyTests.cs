using FluentAssertions;
using TriggerArgs = Nalu.SharpState.Tests.Runtime.TestTriggerArgs;

namespace Nalu.SharpState.Tests.Runtime;

public class HierarchyTests
{
    internal static StateMachineDefinition<TestContext, IServiceProvider, HierState, HierTrigger, TestActor> BuildHier(
        Action<InternalEnumMap<HierState, TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>>> setup)
    {
        var map = new InternalEnumMap<HierState, TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>>();
        map[HierState.Idle] = new();
        map[HierState.Connected] = new();
        map[HierState.Authenticating] = new();
        map[HierState.Authenticated] = new();
        map[HierState.Outside] = new();
        setup(map);
        var forDef = new InternalEnumMap<HierState, IStateConfiguration<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        return new StateMachineDefinition<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(forDef);
    }

    internal static StateMachineDefinition<TestContext, IServiceProvider, HierState, HierTrigger, TestActor> CreateStandardHierarchy()
        => BuildHier(map =>
        {
            map[HierState.Idle].On(
                HierTrigger.Connect,
                TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Connected));
            map[HierState.Connected]
                .AsStateMachine(HierState.Authenticating)
                .On(HierTrigger.Disconnect, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Idle));
            map[HierState.Authenticating]
                .Parent(HierState.Connected)
                .On(HierTrigger.AuthOk, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Authenticated));
            map[HierState.Authenticated]
                .Parent(HierState.Connected)
                .On(HierTrigger.Message, TestTransition.Stay<TestContext, IServiceProvider, HierState, TestActor>(syncAction: (ctx, _, args) => ctx.Log.Add(args.Get<string>(0))));
            map[HierState.Outside].On(
                HierTrigger.GoOutside,
                TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Outside));
        });

    [Fact]
    public void TargetingCompositeDrillsToInitialChild()
    {
        var definition = CreateStandardHierarchy();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Idle, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);

        engine.Fire(HierTrigger.Connect, TriggerArgs.Empty);

        engine.CurrentState.Should().Be(HierState.Authenticating);
    }

    [Fact]
    public void CompositeIsEnteredAsInitialChildWhenUsedAsStartingState()
    {
        var definition = CreateStandardHierarchy();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Connected, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);
        engine.CurrentState.Should().Be(HierState.Authenticating);
    }

    [Fact]
    public void ChildInheritsParentTransitionWhenNotOverridden()
    {
        var definition = CreateStandardHierarchy();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Authenticated, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);

        engine.Fire(HierTrigger.Disconnect, TriggerArgs.Empty);

        engine.CurrentState.Should().Be(HierState.Idle);
    }

    [Fact]
    public void ChildOverridesParentTransition()
    {
        var definition = BuildHier(map =>
        {
            map[HierState.Idle].On(HierTrigger.Connect, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Connected));
            map[HierState.Connected]
                .AsStateMachine(HierState.Authenticating)
                .On(HierTrigger.Disconnect, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Idle));
            map[HierState.Authenticating]
                .Parent(HierState.Connected)
                .On(HierTrigger.Disconnect, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Outside));
            map[HierState.Authenticated].Parent(HierState.Connected);
            map[HierState.Outside].On(HierTrigger.GoOutside, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Outside));
        });

        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Authenticating, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);
        engine.Fire(HierTrigger.Disconnect, TriggerArgs.Empty);
        engine.CurrentState.Should().Be(HierState.Outside);
    }

    [Fact]
    public void IsInTrueForCompositeAncestorAndLeaf()
    {
        var definition = CreateStandardHierarchy();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Authenticated, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);

        engine.IsIn(HierState.Authenticated).Should().BeTrue();
        engine.IsIn(HierState.Connected).Should().BeTrue();
        engine.IsIn(HierState.Idle).Should().BeFalse();
    }

    [Fact]
    public void StayInsideCompositeDoesNotChangeState()
    {
        var definition = CreateStandardHierarchy();
        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Authenticated, ctx, new TestActor(), TestServiceProviders.EmptyResolver);

        engine.Fire(HierTrigger.Message, TriggerArgs.From("hi"));

        engine.CurrentState.Should().Be(HierState.Authenticated);
        ctx.Log.Should().Equal("hi");
    }

    [Fact]
    public void CrossHierarchyTransitionResetsToLeaf()
    {
        var definition = CreateStandardHierarchy();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Authenticated, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);

        engine.Fire(HierTrigger.Disconnect, TriggerArgs.Empty);

        engine.CurrentState.Should().Be(HierState.Idle);
        engine.IsIn(HierState.Connected).Should().BeFalse();
    }

    [Fact]
    public void EnteringCompositeFromOutsideRunsParentThenInitialChild()
    {
        var definition = BuildHier(map =>
        {
            map[HierState.Idle]
                .WhenExiting((ctx, _) => ctx.Log.Add("exit:Idle"))
                .On(HierTrigger.Connect, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Connected));
            map[HierState.Connected]
                .AsStateMachine(HierState.Authenticating)
                .WhenEntering((ctx, _) => ctx.Log.Add("enter:Connected"))
                .On(HierTrigger.Disconnect, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Idle));
            map[HierState.Authenticating]
                .Parent(HierState.Connected)
                .WhenEntering((ctx, _) => ctx.Log.Add("enter:Authenticating"))
                .On(HierTrigger.AuthOk, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Authenticated));
            map[HierState.Authenticated]
                .Parent(HierState.Connected)
                .On(HierTrigger.Message, TestTransition.Stay<TestContext, IServiceProvider, HierState, TestActor>(syncAction: (ctx, _, args) => ctx.Log.Add(args.Get<string>(0))));
            map[HierState.Outside].On(
                HierTrigger.GoOutside,
                TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Outside));
        });

        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Idle, ctx, new TestActor(), TestServiceProviders.EmptyResolver);

        engine.Fire(HierTrigger.Connect, TriggerArgs.Empty);

        ctx.Log.Should().Equal("exit:Idle", "enter:Connected", "enter:Authenticating");
    }

    [Fact]
    public void LeavingCompositeToExternalStateRunsExitsUpChainThenTargetEntry()
    {
        var definition = BuildHier(map =>
        {
            map[HierState.Idle]
                .WhenEntering((ctx, _) => ctx.Log.Add("enter:Idle"))
                .On(HierTrigger.Connect, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Connected));
            map[HierState.Connected]
                .AsStateMachine(HierState.Authenticating)
                .WhenExiting((ctx, _) => ctx.Log.Add("exit:Connected"))
                .On(HierTrigger.Disconnect, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Idle));
            map[HierState.Authenticating]
                .Parent(HierState.Connected)
                .On(HierTrigger.AuthOk, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Authenticated));
            map[HierState.Authenticated]
                .Parent(HierState.Connected)
                .WhenExiting((ctx, _) => ctx.Log.Add("exit:Authenticated"))
                .On(HierTrigger.Message, TestTransition.Stay<TestContext, IServiceProvider, HierState, TestActor>(syncAction: (ctx, _, args) => ctx.Log.Add(args.Get<string>(0))));
            map[HierState.Outside].On(
                HierTrigger.GoOutside,
                TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Outside));
        });

        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Authenticated, ctx, new TestActor(), TestServiceProviders.EmptyResolver);

        engine.Fire(HierTrigger.Disconnect, TriggerArgs.Empty);

        ctx.Log.Should().Equal("exit:Authenticated", "exit:Connected", "enter:Idle");
    }

    [Fact]
    public void ExitAndEntryActionsFollowHierarchyPath()
    {
        var definition = BuildHier(map =>
        {
            map[HierState.Idle].On(HierTrigger.Connect, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Connected));
            map[HierState.Connected]
                .AsStateMachine(HierState.Authenticating)
                .WhenExiting((ctx, _) => ctx.Log.Add("exit:Connected"))
                .On(HierTrigger.Disconnect, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Idle));
            map[HierState.Authenticating]
                .Parent(HierState.Connected)
                .WhenExiting((ctx, _) => ctx.Log.Add("exit:Authenticating"))
                .On(HierTrigger.AuthOk, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Authenticated));
            map[HierState.Authenticated]
                .Parent(HierState.Connected)
                .WhenEntering((ctx, _) => ctx.Log.Add("enter:Authenticated"));
            map[HierState.Outside].On(
                HierTrigger.GoOutside,
                TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Outside));
        });

        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Authenticating, ctx, new TestActor(), TestServiceProviders.EmptyResolver);

        engine.Fire(HierTrigger.AuthOk, TriggerArgs.Empty);

        ctx.Log.Should().Equal("exit:Authenticating", "enter:Authenticated");
    }

    [Fact]
    public async Task FireAsyncRunsWhenExitedAsyncForLeafAndAncestorWhenLeavingNestedComposite()
    {
        var definition = BuildHier(map =>
        {
            map[HierState.Idle].On(
                HierTrigger.Connect,
                TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Connected));
            map[HierState.Connected]
                .AsStateMachine(HierState.Authenticating)
                .WhenExitedAsync((ctx, _) =>
                {
                    ctx.Log.Add("exitedAsync:Connected");
                    return ValueTask.CompletedTask;
                })
                .On(HierTrigger.Disconnect, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Idle));
            map[HierState.Authenticating]
                .Parent(HierState.Connected)
                .On(HierTrigger.AuthOk, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Authenticated));
            map[HierState.Authenticated]
                .Parent(HierState.Connected)
                .WhenExitedAsync((ctx, _) =>
                {
                    ctx.Log.Add("exitedAsync:Authenticated");
                    return ValueTask.CompletedTask;
                })
                .On(HierTrigger.Disconnect, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Idle));
            map[HierState.Outside].On(
                HierTrigger.GoOutside,
                TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Outside));
        });

        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(
            definition,
            HierState.Authenticated,
            ctx,
            new TestActor(),
            TestServiceProviders.EmptyResolver);

        await engine.FireAsync(HierTrigger.Disconnect, TriggerArgs.Empty);

        ctx.Log.Should().Equal("exitedAsync:Authenticated", "exitedAsync:Connected");
        engine.CurrentState.Should().Be(HierState.Idle);
    }

    [Fact]
    public async Task FireAsyncRunsWhenEnteredAsyncForAncestorWhenEnteringNestedComposite()
    {
        var definition = BuildHier(map =>
        {
            map[HierState.Idle].On(
                HierTrigger.Connect,
                TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Connected));
            map[HierState.Connected]
                .AsStateMachine(HierState.Authenticating)
                .WhenEnteredAsync((ctx, _) =>
                {
                    ctx.Log.Add("enteredAsync:Connected");
                    return ValueTask.CompletedTask;
                })
                .On(HierTrigger.Disconnect, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Idle));
            map[HierState.Authenticating]
                .Parent(HierState.Connected)
                .WhenEnteredAsync((ctx, _) =>
                {
                    ctx.Log.Add("enteredAsync:Authenticating");
                    return ValueTask.CompletedTask;
                })
                .On(HierTrigger.AuthOk, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Authenticated));
            map[HierState.Authenticated]
                .Parent(HierState.Connected)
                .On(HierTrigger.Message, TestTransition.Stay<TestContext, IServiceProvider, HierState, TestActor>());
            map[HierState.Outside].On(
                HierTrigger.GoOutside,
                TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Outside));
        });

        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(
            definition,
            HierState.Idle,
            ctx,
            new TestActor(),
            TestServiceProviders.EmptyResolver);

        await engine.FireAsync(HierTrigger.Connect, TriggerArgs.Empty);

        ctx.Log.Should().Equal("enteredAsync:Connected", "enteredAsync:Authenticating");
        engine.CurrentState.Should().Be(HierState.Authenticating);
    }
}
