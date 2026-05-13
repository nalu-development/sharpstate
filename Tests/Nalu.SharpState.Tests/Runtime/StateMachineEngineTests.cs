using FluentAssertions;
using System.Reflection;
using TriggerArgs = Nalu.SharpState.Tests.Runtime.TestTriggerArgs;

namespace Nalu.SharpState.Tests.Runtime;

public class StateMachineEngineTests
{
    private static StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor> BuildFlat(
        Action<InternalEnumMap<FlatState, TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>>? setup = null)
    {
        var map = new InternalEnumMap<FlatState, TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new();
        map[FlatState.B] = new();
        map[FlatState.C] = new();
        setup?.Invoke(map);

        var forDef = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        return new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(forDef);
    }

    [Fact]
    public void Fire_flat_transition_moves_current_state_and_raises_StateChanged()
    {
        var definition = BuildFlat(map =>
        {
            map[FlatState.A].On(
                FlatTrigger.Go,
                TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.B));
        });

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(definition, FlatState.A, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);
        (FlatState from, FlatState to, FlatTrigger trigger, IServiceProvider args)? captured = null;
        engine.StateChanged += (from, to, trig, args) => captured = (from, to, trig, args);

        engine.Fire(FlatTrigger.Go, TriggerArgs.From(42, "payload"));

        engine.CurrentState.Should().Be(FlatState.B);
        captured.Should().NotBeNull();
        captured!.Value.from.Should().Be(FlatState.A);
        captured.Value.to.Should().Be(FlatState.B);
        captured.Value.trigger.Should().Be(FlatTrigger.Go);
        captured.Value.args.Get<int>(0).Should().Be(42);
        captured.Value.args.Get<string>(1).Should().Be("payload");
    }

    [Fact]
    public void Fire_unhandled_trigger_invokes_OnUnhandled()
    {
        var definition = BuildFlat();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(definition, FlatState.A, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);
        (FlatState state, FlatTrigger trigger, IServiceProvider args)? captured = null;
        engine.OnUnhandled = (s, t, a) => captured = (s, t, a);

        engine.Fire(FlatTrigger.NoMatch, TriggerArgs.From(123));

        captured.Should().NotBeNull();
        captured!.Value.state.Should().Be(FlatState.A);
        captured.Value.trigger.Should().Be(FlatTrigger.NoMatch);
        captured.Value.args.Get<int>(0).Should().Be(123);
        engine.CurrentState.Should().Be(FlatState.A);
    }

    [Fact]
    public void CanFire_returns_true_when_transition_matches_current_state()
    {
        var definition = BuildFlat(map =>
        {
            map[FlatState.A].On(
                FlatTrigger.Go,
                TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.B));
        });

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(definition, FlatState.A, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);

        engine.CanFire(FlatTrigger.Go, TriggerArgs.Empty).Should().BeTrue();
        engine.CanFire(FlatTrigger.NoMatch, TriggerArgs.Empty).Should().BeFalse();
    }

    [Fact]
    public void CanFire_respects_guards_without_mutating_state()
    {
        var definition = BuildFlat(map =>
        {
            map[FlatState.A].On(
                FlatTrigger.Go,
                TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                    FlatState.B,
                    guard: (ctx, _, args) => ctx.Counter == args.Get<int>(0)));
        });

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            definition,
            FlatState.A,
            new TestContext { Counter = 4 },
            new TestActor(),
            TestServiceProviders.EmptyResolver);

        engine.CanFire(FlatTrigger.Go, TriggerArgs.From(3)).Should().BeFalse();
        engine.CanFire(FlatTrigger.Go, TriggerArgs.From(4)).Should().BeTrue();
        engine.CurrentState.Should().Be(FlatState.A);
    }

    [Fact]
    public void Fire_unhandled_with_default_callback_throws()
    {
        var definition = BuildFlat();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(definition, FlatState.A, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);

        var act = () => engine.Fire(FlatTrigger.NoMatch, TriggerArgs.Empty);

        act.Should().Throw<InvalidOperationException>();
        engine.CurrentState.Should().Be(FlatState.A);
    }

    [Fact]
    public void Fire_unhandled_with_null_callback_is_silent_noop()
    {
        var definition = BuildFlat();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(definition, FlatState.A, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver)
        {
            OnUnhandled = null,
        };

        var act = () => engine.Fire(FlatTrigger.NoMatch, TriggerArgs.Empty);

        act.Should().NotThrow();
        engine.CurrentState.Should().Be(FlatState.A);
    }

    [Fact]
    public void Fire_first_guarded_transition_that_passes_wins()
    {
        var definition = BuildFlat(map =>
        {
            map[FlatState.A].AddAllFor(FlatTrigger.Go, [
                new Transition<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.B, null, false, (ctx, _, _) => ctx.Counter > 10, null, null, null),
                new Transition<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.C, null, false, null, null, null, null)
            ]);
        });

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(definition, FlatState.A, new TestContext { Counter = 5 }, new TestActor(), TestServiceProviders.EmptyResolver);
        engine.Fire(FlatTrigger.Go, TriggerArgs.Empty);
        engine.CurrentState.Should().Be(FlatState.C);
    }

    [Fact]
    public void Fire_guard_receives_args_and_context()
    {
        var definition = BuildFlat(map =>
        {
            map[FlatState.A].On(
                FlatTrigger.Go,
                TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                    FlatState.B,
                    guard: (ctx, _, args) => args.Get<int>(0) == ctx.Counter));
        });

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(definition, FlatState.A, new TestContext { Counter = 42 }, new TestActor(), TestServiceProviders.EmptyResolver)
        {
            OnUnhandled = null,
        };
        engine.Fire(FlatTrigger.Go, TriggerArgs.From(41));
        engine.CurrentState.Should().Be(FlatState.A);

        engine.Fire(FlatTrigger.Go, TriggerArgs.From(42));
        engine.CurrentState.Should().Be(FlatState.B);
    }

    [Fact]
    public void Fire_runs_action_before_state_change_and_event()
    {
        var definition = BuildFlat(map =>
        {
            map[FlatState.A].On(
                FlatTrigger.Go,
                TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                    FlatState.B,
                    syncAction: (ctx, _, _) => ctx.Log.Add("action:" + ctx.Counter)));
        });

        var ctx = new TestContext { Counter = 1 };
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(definition, FlatState.A, ctx, new TestActor(), TestServiceProviders.EmptyResolver);
        engine.StateChanged += (_, _, _, _) => ctx.Log.Add("changed:" + ctx.Counter);

        engine.Fire(FlatTrigger.Go, TriggerArgs.Empty);

        ctx.Log.Should().BeEquivalentTo("action:1", "changed:1");
    }

    [Fact]
    public void Fire_dynamic_target_uses_context_and_args()
    {
        var definition = BuildFlat(map =>
        {
            map[FlatState.A].On(
                FlatTrigger.Go,
                TestTransition.ToDynamicTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                    (ctx, _, args) => args.Get<int>(0) == ctx.Counter ? FlatState.B : FlatState.C));
        });

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            definition,
            FlatState.A,
            new TestContext { Counter = 7 },
            new TestActor(),
            TestServiceProviders.EmptyResolver);

        engine.Fire(FlatTrigger.Go, TriggerArgs.From(7));
        engine.CurrentState.Should().Be(FlatState.B);
    }

    [Fact]
    public void Fire_dynamic_external_transition_invokes_sync_action_and_StateChanged()
    {
        var definition = BuildFlat(map =>
        {
            map[FlatState.A].On(
                FlatTrigger.Go,
                TestTransition.ToDynamicTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                    (_, _, args) => args.Get<bool>(0) ? FlatState.B : FlatState.C,
                    syncAction: (ctx, _, _) => ctx.Log.Add("dyn-sync")));
            map[FlatState.B].WhenEntering((ctx, _) => ctx.Log.Add("enter:B"));
            map[FlatState.C].WhenEntering((ctx, _) => ctx.Log.Add("enter:C"));
        });

        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            definition,
            FlatState.A,
            ctx,
            new TestActor(),
            TestServiceProviders.EmptyResolver);
        engine.StateChanged += (_, _, _, _) => ctx.Log.Add("changed");

        engine.Fire(FlatTrigger.Go, TriggerArgs.From(true));

        engine.CurrentState.Should().Be(FlatState.B);
        ctx.Log.Should().Equal("dyn-sync", "enter:B", "changed");
    }

    [Fact]
    public void Fire_dynamic_target_to_current_state_behaves_like_internal_transition()
    {
        var definition = BuildFlat(map =>
        {
            map[FlatState.A]
                .WhenEntering((ctx, _) => ctx.Log.Add("enter:A"))
                .WhenExiting((ctx, _) => ctx.Log.Add("exit:A"))
                .On(
                    FlatTrigger.Go,
                    TestTransition.ToDynamicTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                        (_, _, args) => args.Get<bool>(0) ? FlatState.A : FlatState.B,
                        syncAction: (ctx, _, _) => ctx.Log.Add("invoke")));
            map[FlatState.B]
                .WhenEntering((ctx, _) => ctx.Log.Add("enter:B"));
        });

        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(definition, FlatState.A, ctx, new TestActor(), TestServiceProviders.EmptyResolver);
        var changed = false;
        engine.StateChanged += (_, _, _, _) => changed = true;

        engine.Fire(FlatTrigger.Go, TriggerArgs.From(true));

        engine.CurrentState.Should().Be(FlatState.A);
        changed.Should().BeFalse();
        ctx.Log.Should().Equal("invoke");
    }

    [Fact]
    public void Fire_when_all_guards_fail_invokes_unhandled()
    {
        var definition = BuildFlat(map =>
        {
            map[FlatState.A].AddAllFor(FlatTrigger.Go, [
                new Transition<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.B, null, false, (_, _, _) => false, null, null, null),
                new Transition<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.C, null, false, (_, _, _) => false, null, null, null)
            ]);
        });

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(definition, FlatState.A, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);
        FlatState? capturedState = null;
        engine.OnUnhandled = (state, _, _) => capturedState = state;

        engine.Fire(FlatTrigger.Go, TriggerArgs.Empty);

        capturedState.Should().Be(FlatState.A);
        engine.CurrentState.Should().Be(FlatState.A);
    }

    [Fact]
    public void Fire_walks_to_parent_when_leaf_has_no_matching_transition()
    {
        var definition = HierarchyTests.CreateStandardHierarchy();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Authenticated, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);

        engine.Fire(HierTrigger.Disconnect, TriggerArgs.Empty);

        engine.CurrentState.Should().Be(HierState.Idle);
    }

    [Fact]
    public void Fire_walks_to_parent_when_leaf_state_has_no_entry_for_trigger()
    {
        var definition = HierarchyTests.BuildHier(map =>
        {
            map[HierState.Idle].On(
                HierTrigger.Connect,
                TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Connected));
            map[HierState.Connected]
                .AsStateMachine(HierState.Authenticating)
                .On(HierTrigger.Disconnect, TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Idle));
            map[HierState.Authenticating].Parent(HierState.Connected);
            map[HierState.Authenticated]
                .Parent(HierState.Connected)
                .On(HierTrigger.Message, TestTransition.Stay<TestContext, IServiceProvider, HierState, TestActor>());
            map[HierState.Outside].On(
                HierTrigger.GoOutside,
                TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(HierState.Outside));
        });

        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(
            definition,
            HierState.Authenticated,
            new TestContext(),
            new TestActor(),
            TestServiceProviders.EmptyResolver);

        engine.Fire(HierTrigger.Disconnect, TriggerArgs.Empty);

        engine.CurrentState.Should().Be(HierState.Idle);
    }

    [Fact]
    public void Fire_runs_exit_then_entry_actions_around_external_transition()
    {
        var definition = BuildFlat(map =>
        {
            map[FlatState.A]
                .WhenExiting((ctx, _) => ctx.Log.Add("exit:A"))
                .On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.B));
            map[FlatState.B]
                .WhenEntering((ctx, _) => ctx.Log.Add("enter:B"));
        });

        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(definition, FlatState.A, ctx, new TestActor(), TestServiceProviders.EmptyResolver);

        engine.Fire(FlatTrigger.Go, TriggerArgs.Empty);

        ctx.Log.Should().Equal("exit:A", "enter:B");
    }

    [Fact]
    public void CanFire_returns_false_when_current_leaf_has_no_definition_entry()
    {
        var map = new InternalEnumMap<FlatState, TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>()
            .On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.B));
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();

        var forDef = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        var definition = new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(forDef);
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            definition,
            FlatState.A,
            new TestContext(),
            new TestActor(),
            TestServiceProviders.EmptyResolver);

        var currentField = typeof(StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>).GetField(
            "_currentState",
            BindingFlags.Instance | BindingFlags.NonPublic);
        currentField.Should().NotBeNull();
        currentField!.SetValue(engine, FlatState.C);

        engine.CanFire(FlatTrigger.Go, TriggerArgs.Empty).Should().BeFalse();
    }

    [Fact]
    public void IndexOf_returns_negative_when_state_not_in_ancestor_chain()
    {
        var indexOf = typeof(StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>).GetMethod(
            "IndexOf",
            BindingFlags.NonPublic | BindingFlags.Static);
        indexOf.Should().NotBeNull();
        var chain = new[] { FlatState.A, FlatState.B };
        ((int)indexOf!.Invoke(null, [chain, FlatState.C])!).Should().Be(-1);
    }

    [Fact]
    public void Constructor_throws_when_definition_is_null()
    {
        var act = () => new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            null!,
            FlatState.A,
            new TestContext(),
            new TestActor(),
            TestServiceProviders.EmptyResolver);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("definition");
    }

    [Fact]
    public void Constructor_throws_when_service_provider_resolver_is_null()
    {
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        var def = new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map);
        var act = () => new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            def,
            FlatState.A,
            new TestContext(),
            new TestActor(),
            null!);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("serviceProviderResolver");
    }

    [Fact]
    public void Constructor_throws_when_context_is_null()
    {
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.C] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        var def = new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map);
        var act = () => new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(def, FlatState.A, null!, new TestActor(), TestServiceProviders.EmptyResolver);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void Constructor_throws_when_initial_state_is_not_registered()
    {
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        var def = new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map);
        var act = () => new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(def, FlatState.C, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);
        act.Should().ThrowExactly<KeyNotFoundException>().WithMessage("*not registered*");
    }

    [Fact]
    public void Fire_dynamic_to_same_leaf_invokes_SyncAction()
    {
        var definition = BuildFlat(map =>
        {
            map[FlatState.A]
                .On(
                    FlatTrigger.Go,
                    TestTransition.ToDynamicTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                        (_, _, args) => args.Get<bool>(0) ? FlatState.A : FlatState.B,
                        syncAction: (ctx, _, _) => ctx.Log.Add("inner")));
            map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        });

        var ctx = new TestContext();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(definition, FlatState.A, ctx, new TestActor(), TestServiceProviders.EmptyResolver);

        engine.Fire(FlatTrigger.Go, TriggerArgs.From(true));

        engine.CurrentState.Should().Be(FlatState.A);
        ctx.Log.Should().Equal("inner");
    }

    [Fact]
    public void Fire_throws_when_reentered_from_callback()
    {
        StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>? engine = null;
        var definition = BuildFlat(map =>
        {
            map[FlatState.A]
                .On(FlatTrigger.Go, TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.B));
            map[FlatState.B]
                .WhenEntering((_, _) => engine!.Fire(FlatTrigger.NoMatch, TriggerArgs.Empty));
        });

        engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(definition, FlatState.A, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver)
        {
            OnUnhandled = null,
        };

        var act = () => engine.Fire(FlatTrigger.Go, TriggerArgs.Empty);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be fired while another trigger is still being processed*");
        engine.CurrentState.Should().Be(FlatState.B);
    }
}

internal static class TestConfiguratorExtensions
{
    public static TestStateConfigurator<TContext, TArgs, TState, TTrigger, TActor> AddAllFor<TContext, TArgs, TState, TTrigger, TActor>(
        this TestStateConfigurator<TContext, TArgs, TState, TTrigger, TActor> self,
        TTrigger trigger,
        IReadOnlyList<Transition<TContext, TArgs, TState, TActor>> transitions)
        where TContext : class
        where TState : struct, Enum
        where TTrigger : struct, Enum
    {
        foreach (var t in transitions)
        {
            self.On(trigger, t);
        }

        return self;
    }
}
