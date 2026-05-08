using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class HierarchyTests
{
    private static StateMachineDefinition<TestContext, IServiceProvider, HierState, HierTrigger, TestActor> BuildHier(
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
    public void Targeting_composite_drills_to_initial_child()
    {
        var definition = CreateStandardHierarchy();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Idle, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);

        engine.Fire(HierTrigger.Connect, TriggerArgs.Empty);

        engine.CurrentState.Should().Be(HierState.Authenticating);
    }

    [Fact]
    public void Composite_is_entered_as_initial_child_when_used_as_starting_state()
    {
        var definition = CreateStandardHierarchy();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Connected, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);
        engine.CurrentState.Should().Be(HierState.Authenticating);
    }

    [Fact]
    public void Child_inherits_parent_transition_when_not_overridden()
    {
        var definition = CreateStandardHierarchy();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Authenticated, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);

        engine.Fire(HierTrigger.Disconnect, TriggerArgs.Empty);

        engine.CurrentState.Should().Be(HierState.Idle);
    }

    [Fact]
    public void Child_overrides_parent_transition()
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
    public void IsIn_true_for_composite_ancestor_and_leaf()
    {
        var definition = CreateStandardHierarchy();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Authenticated, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);

        engine.IsIn(HierState.Authenticated).Should().BeTrue();
        engine.IsIn(HierState.Connected).Should().BeTrue();
        engine.IsIn(HierState.Idle).Should().BeFalse();
    }

    [Fact]
    public void Stay_inside_composite_does_not_change_state()
    {
        var definition = CreateStandardHierarchy();
        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Authenticated, ctx, new TestActor(), TestServiceProviders.EmptyResolver);

        engine.Fire(HierTrigger.Message, TriggerArgs.From("hi"));

        engine.CurrentState.Should().Be(HierState.Authenticated);
        ctx.Log.Should().Equal("hi");
    }

    [Fact]
    public void Cross_hierarchy_transition_resets_to_leaf()
    {
        var definition = CreateStandardHierarchy();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Authenticated, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);

        engine.Fire(HierTrigger.Disconnect, TriggerArgs.Empty);

        engine.CurrentState.Should().Be(HierState.Idle);
        engine.IsIn(HierState.Connected).Should().BeFalse();
    }

    [Fact]
    public void Entering_composite_from_outside_runs_parent_then_initial_child()
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
    public void Leaving_composite_to_external_state_runs_exits_up_chain_then_target_entry()
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
    public void Exit_and_entry_actions_follow_hierarchy_path()
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
}
