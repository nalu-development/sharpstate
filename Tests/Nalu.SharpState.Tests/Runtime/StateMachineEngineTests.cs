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
    public void FireFlatTransitionMovesCurrentStateAndRaisesStateChanged()
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
    public void FireUnhandledTriggerInvokesOnUnhandled()
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
    public void CanFireReturnsTrueWhenTransitionMatchesCurrentState()
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
    public void CanFireRespectsGuardsWithoutMutatingState()
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
    public void FireUnhandledWithDefaultCallbackThrows()
    {
        var definition = BuildFlat();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(definition, FlatState.A, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);

        var act = () => engine.Fire(FlatTrigger.NoMatch, TriggerArgs.Empty);

        act.Should().Throw<InvalidOperationException>();
        engine.CurrentState.Should().Be(FlatState.A);
    }

    [Fact]
    public void FireUnhandledWithNullCallbackIsSilentNoop()
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
    public void FireFirstGuardedTransitionThatPassesWins()
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
    public void FireGuardReceivesArgsAndContext()
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
    public void FireRunsActionBeforeStateChangeAndEvent()
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
    public void FireDynamicTargetUsesContextAndArgs()
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
    public void FireDynamicExternalTransitionInvokesSyncActionAndStateChanged()
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
    public void FireDynamicTargetToCurrentStateBehavesLikeInternalTransition()
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
    public void FireWhenAllGuardsFailInvokesUnhandled()
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
    public void FireWalksToParentWhenLeafHasNoMatchingTransition()
    {
        var definition = HierarchyTests.CreateStandardHierarchy();
        var engine = new StateMachineEngine<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(definition, HierState.Authenticated, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);

        engine.Fire(HierTrigger.Disconnect, TriggerArgs.Empty);

        engine.CurrentState.Should().Be(HierState.Idle);
    }

    [Fact]
    public void FireWalksToParentWhenLeafStateHasNoEntryForTrigger()
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
    public void FireRunsExitThenEntryActionsAroundExternalTransition()
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
    public void CanFireReturnsFalseWhenCurrentLeafHasNoDefinitionEntry()
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
        currentField.SetValue(engine, FlatState.C);

        engine.CanFire(FlatTrigger.Go, TriggerArgs.Empty).Should().BeFalse();
    }

    [Fact]
    public void IndexOfReturnsNegativeWhenStateNotInAncestorChain()
    {
        var indexOf = typeof(StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>).GetMethod(
            "IndexOf",
            BindingFlags.NonPublic | BindingFlags.Static);
        indexOf.Should().NotBeNull();
        var chain = new[] { FlatState.A, FlatState.B };
        ((int)indexOf.Invoke(null, [chain, FlatState.C])!).Should().Be(-1);
    }

    [Fact]
    public void ConstructorThrowsWhenDefinitionIsNull()
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
    public void ConstructorThrowsWhenServiceProviderResolverIsNull()
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
    public void ConstructorThrowsWhenContextIsNull()
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
    public void ConstructorThrowsWhenInitialStateIsNotRegistered()
    {
        var map = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        map[FlatState.B] = new TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>();
        var def = new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(map);
        var act = () => new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(def, FlatState.C, new TestContext(), new TestActor(), TestServiceProviders.EmptyResolver);
        act.Should().ThrowExactly<KeyNotFoundException>().WithMessage("*not registered*");
    }

    [Fact]
    public void FireDynamicToSameLeafInvokesSyncAction()
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
    public void FireThrowsWhenReenteredFromCallback()
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
