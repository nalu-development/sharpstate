using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class StateAwareContextTests
{
    private static StateMachineDefinition<StateAwareTestContext<FlatState>, FlatState, FlatTrigger, TestActor> BuildFlat(
        Action<InternalEnumMap<FlatState, TestStateConfigurator<StateAwareTestContext<FlatState>, FlatState, FlatTrigger, TestActor>>>? setup = null)
    {
        var map = new InternalEnumMap<FlatState, TestStateConfigurator<StateAwareTestContext<FlatState>, FlatState, FlatTrigger, TestActor>>
                  {
                      [FlatState.A] = new(),
                      [FlatState.B] = new(),
                      [FlatState.C] = new()
                  };

        setup?.Invoke(map);

        var forDef = new InternalEnumMap<FlatState, IStateConfiguration<StateAwareTestContext<FlatState>, FlatState, FlatTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        return new StateMachineDefinition<StateAwareTestContext<FlatState>, FlatState, FlatTrigger, TestActor>(forDef);
    }

    private static StateMachineDefinition<StateAwareTestContext<HierState>, HierState, HierTrigger, TestActor> BuildStandardHierarchyStateAware()
    {
        var map = new InternalEnumMap<HierState, TestStateConfigurator<StateAwareTestContext<HierState>, HierState, HierTrigger, TestActor>>
                  {
                      [HierState.Idle] = new(),
                      [HierState.Connected] = new(),
                      [HierState.Authenticating] = new(),
                      [HierState.Authenticated] = new(),
                      [HierState.Outside] = new()
                  };

        map[HierState.Idle].On(
            HierTrigger.Connect,
            TestTransition.ToTarget<StateAwareTestContext<HierState>, HierState, TestActor>(HierState.Connected));
        map[HierState.Connected]
            .AsStateMachine(HierState.Authenticating)
            .On(HierTrigger.Disconnect, TestTransition.ToTarget<StateAwareTestContext<HierState>, HierState, TestActor>(HierState.Idle));
        map[HierState.Authenticating]
            .Parent(HierState.Connected)
            .On(HierTrigger.AuthOk, TestTransition.ToTarget<StateAwareTestContext<HierState>, HierState, TestActor>(HierState.Authenticated));
        map[HierState.Authenticated]
            .Parent(HierState.Connected)
            .On(
                HierTrigger.Message,
                TestTransition.Stay<StateAwareTestContext<HierState>, HierState, TestActor>(
                    syncAction: (ctx, args) => ctx.Log.Add(args.Get<string>(0))));
        map[HierState.Outside].On(
            HierTrigger.GoOutside,
            TestTransition.ToTarget<StateAwareTestContext<HierState>, HierState, TestActor>(HierState.Outside));

        var forDef = new InternalEnumMap<HierState, IStateConfiguration<StateAwareTestContext<HierState>, HierState, HierTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        return new StateMachineDefinition<StateAwareTestContext<HierState>, HierState, HierTrigger, TestActor>(forDef);
    }

    [Fact]
    public void OnStateChanged_receives_leaf_state_after_external_transition()
    {
        var definition = BuildFlat(map =>
        {
            map[FlatState.A].On(
                FlatTrigger.Go,
                TestTransition.ToTarget<StateAwareTestContext<FlatState>, FlatState, TestActor>(FlatState.B));
        });

        var ctx = new StateAwareTestContext<FlatState>();
        var engine = new StateMachineEngine<StateAwareTestContext<FlatState>, FlatState, FlatTrigger, TestActor>(
            definition,
            FlatState.A,
            ctx,
            new TestActor());

        engine.Fire(FlatTrigger.Go, TriggerArgs.Empty);

        ctx.NotifiedStates.Should().Equal(FlatState.B);
        engine.CurrentState.Should().Be(FlatState.B);
    }

    [Fact]
    public void OnStateChanged_runs_after_entry_actions_and_before_StateChanged_event()
    {
        var definition = BuildFlat(map =>
        {
            map[FlatState.A].On(
                FlatTrigger.Go,
                TestTransition.ToTarget<StateAwareTestContext<FlatState>, FlatState, TestActor>(FlatState.B));
            map[FlatState.B].WhenEntering(c => c.Log.Add("enter:B"));
        });

        var ctx = new StateAwareTestContext<FlatState>();
        var engine = new StateMachineEngine<StateAwareTestContext<FlatState>, FlatState, FlatTrigger, TestActor>(
            definition,
            FlatState.A,
            ctx,
            new TestActor());

        engine.StateChanged += (_, _, _, _) => ctx.Log.Add("changed");

        engine.Fire(FlatTrigger.Go, TriggerArgs.Empty);

        ctx.Log.Should().Equal("enter:B", "notify:B", "changed");
    }

    [Fact]
    public void OnStateChanged_is_not_invoked_for_internal_Stay_transition()
    {
        var definition = BuildFlat(map =>
        {
            map[FlatState.A]
                .On(FlatTrigger.Go, TestTransition.Stay<StateAwareTestContext<FlatState>, FlatState, TestActor>());
        });

        var ctx = new StateAwareTestContext<FlatState>();
        var engine = new StateMachineEngine<StateAwareTestContext<FlatState>, FlatState, FlatTrigger, TestActor>(
            definition,
            FlatState.A,
            ctx,
            new TestActor());

        engine.Fire(FlatTrigger.Go, TriggerArgs.Empty);

        ctx.NotifiedStates.Should().BeEmpty();
        engine.CurrentState.Should().Be(FlatState.A);
    }

    [Fact]
    public void OnStateChanged_is_not_invoked_when_dynamic_target_resolves_to_current_leaf()
    {
        var definition = BuildFlat(map =>
        {
            map[FlatState.A]
                .On(
                    FlatTrigger.Go,
                    TestTransition.ToDynamicTarget<StateAwareTestContext<FlatState>, FlatState, TestActor>(
                        (_, args) => args.Get<bool>(0) ? FlatState.A : FlatState.B));
        });

        var ctx = new StateAwareTestContext<FlatState>();
        var engine = new StateMachineEngine<StateAwareTestContext<FlatState>, FlatState, FlatTrigger, TestActor>(
            definition,
            FlatState.A,
            ctx,
            new TestActor());

        engine.Fire(FlatTrigger.Go, TriggerArgs.From(true));

        ctx.NotifiedStates.Should().BeEmpty();
        engine.CurrentState.Should().Be(FlatState.A);
    }

    [Fact]
    public void OnStateChanged_is_not_invoked_by_constructor()
    {
        var definition = BuildFlat();
        var ctx = new StateAwareTestContext<FlatState>();
        _ = new StateMachineEngine<StateAwareTestContext<FlatState>, FlatState, FlatTrigger, TestActor>(
            definition,
            FlatState.A,
            ctx,
            new TestActor());

        ctx.NotifiedStates.Should().BeEmpty();
    }

    [Fact]
    public void OnStateChanged_receives_resolved_leaf_in_hierarchical_machine()
    {
        var definition = BuildStandardHierarchyStateAware();
        var ctx = new StateAwareTestContext<HierState>();
        var engine = new StateMachineEngine<StateAwareTestContext<HierState>, HierState, HierTrigger, TestActor>(
            definition,
            HierState.Authenticated,
            ctx,
            new TestActor());

        engine.Fire(HierTrigger.Disconnect, TriggerArgs.Empty);

        ctx.NotifiedStates.Should().Equal(HierState.Idle);
        engine.CurrentState.Should().Be(HierState.Idle);
    }
}
