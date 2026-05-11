using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class StateTriggerArgsBuilderTests
{
    [Fact]
    public void Validate_requires_Target_or_Stay()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();

        var act = builder.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Target*Stay*");
    }

    [Fact]
    public void BuildTransitions_produces_single_transition_with_target()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();

        builder.Target(FlatState.B);
        builder.Validate();

        var transition = builder.BuildTransitions().Should().ContainSingle().Subject;
        transition.IsInternal.Should().BeFalse();
        transition.Target.Should().Be(FlatState.B);
    }

    [Fact]
    public void Builder_uses_typed_payload_for_guards_actions_and_dynamic_targets()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        builder
            .When((ctx, args) => ctx.Counter == args.Get<int>(0), "counter matches")
            .Target((_, args) => args.Get<string>(1) == "b" ? FlatState.B : FlatState.C)
            .Invoke((ctx, args) => ctx.LastArg = args.Get<string>(1));
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        var context = new TestContext { Counter = 5 };
        var args = TestTriggerArgs.From(5, "b");

        transition.Guard!(context, EmptyServiceProvider.Instance, args).Should().BeTrue();
        transition.TargetSelector!(context, EmptyServiceProvider.Instance, args).Should().Be(FlatState.B);
        transition.SyncAction!(context, EmptyServiceProvider.Instance, args);
        context.LastArg.Should().Be("b");
        transition.GuardLabels.Should().Equal("counter matches");
    }

    [Fact]
    public async Task ReactAsync_stores_background_reaction()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        builder.Target(FlatState.B)
            .ReactAsync(async (_, ctx, args) =>
            {
                await Task.Yield();
                ctx.Counter += args.Get<int>(0);
            });
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        var context = new TestContext { Counter = 10 };
        await transition.ReactionAsync!(new TestActor(), context, EmptyServiceProvider.Instance, TestTriggerArgs.From(5));

        context.Counter.Should().Be(15);
    }

    [Fact]
    public void Ignore_is_syntax_sugar_for_internal_transition()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();

        builder.Ignore();
        builder.Validate();

        builder.BuildTransitions()[0].IsInternal.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_throws_when_target_and_stay_are_both_set(bool targetFirst)
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        if (targetFirst)
        {
            builder.Target(FlatState.B);
            builder.Stay();
        }
        else
        {
            builder.Stay();
            builder.Target(FlatState.B);
        }

        var act = builder.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Target*Stay*");
    }

    [Fact]
    public void Constructor_getArgs_throws_on_null()
    {
        var act = () => new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("getArgs");
    }

    [Fact]
    public void Target_selector_throws_on_null_delegate()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();

        var act = () => builder.Target(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("targetSelector");
    }

    [Fact]
    public void When_throws_on_null_delegate()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();

        var act = () => builder.When(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("guard");
    }

    [Fact]
    public void Invoke_throws_on_null_delegate()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();

        var act = () => builder.Invoke(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("action");
    }

    [Fact]
    public void ReactAsync_throws_on_null_delegate()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();

        var act = () => builder.ReactAsync(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("action");
    }

    [Fact]
    public void When_multiple_guards_are_combined_with_and_short_circuits_and_labels_skip_nulls()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        var secondEvaluated = false;
        builder
            .When((_, _) => false, "first")
            .When((_, _) =>
            {
                secondEvaluated = true;
                return true;
            })
            .When((_, _) => true, label: null)
            .When((_, _) => true, "last")
            .Target(FlatState.B);
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        var context = new TestContext();
        transition.Guard!(context, EmptyServiceProvider.Instance, TestTriggerArgs.Empty).Should().BeFalse();
        secondEvaluated.Should().BeFalse();
        transition.GuardLabels.Should().Equal("first", "last");
    }

    [Fact]
    public void Multiple_When_true_and_true_runs_both_guards()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        var calls = 0;
        builder
            .When((_, _) =>
            {
                calls++;
                return true;
            })
            .When((_, _) =>
            {
                calls++;
                return true;
            })
            .Target(FlatState.B);
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        transition.Guard!(new TestContext(), EmptyServiceProvider.Instance, TestTriggerArgs.Empty).Should().BeTrue();
        calls.Should().Be(2);
    }

    [Fact]
    public void Invoke_multiple_actions_execute_in_declaration_order()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        builder.Target(FlatState.B)
            .Invoke((ctx, _) => ctx.Log.Add("1"))
            .Invoke((ctx, _) => ctx.Log.Add("2"));
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        var context = new TestContext();
        transition.SyncAction!(context, EmptyServiceProvider.Instance, TestTriggerArgs.Empty);

        context.Log.Should().Equal("1", "2");
    }

    [Fact]
    public async Task ReactAsync_multiple_reactions_execute_in_declaration_order()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        builder.Target(FlatState.B)
            .ReactAsync(async (_, ctx, _) =>
            {
                await Task.Yield();
                ctx.Log.Add("1");
            })
            .ReactAsync(async (_, ctx, _) =>
            {
                await Task.Yield();
                ctx.Log.Add("2");
            });
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        var context = new TestContext();
        await transition.ReactionAsync!(new TestActor(), context, EmptyServiceProvider.Instance, TestTriggerArgs.Empty);

        context.Log.Should().Equal("1", "2");
    }

    [Fact]
    public void Target_dynamic_hints_are_stored_when_provided()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        builder.Target((_, _) => FlatState.B, (FlatState.C, "see-c"));
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        transition.DynamicTargetHints.Should().NotBeNull();
        transition.DynamicTargetHints!.Should().ContainSingle()
            .Which.Should().Be((FlatState.C, "see-c"));
    }

    [Fact]
    public void Target_dynamic_hints_are_null_when_not_provided()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        builder.Target((_, _) => FlatState.B);
        builder.Validate();

        builder.BuildTransitions()[0].DynamicTargetHints.Should().BeNull();
    }

    [Fact]
    public void Transition_target_throws_for_internal_transition()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        builder.Stay();
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        var act = () => _ = transition.Target;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Internal*");
    }

    [Fact]
    public void Transition_target_throws_for_dynamic_transition()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        builder.Target((_, _) => FlatState.B);
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        var act = () => _ = transition.Target;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Dynamic*");
    }

    [Fact]
    public void Default_constructor_cast_fails_for_mismatched_machine_and_trigger_arg_types()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, int>();
        builder.When((_, _) => true).Target(FlatState.B);
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        var act = () => transition.Guard!(new TestContext(), EmptyServiceProvider.Instance, TestTriggerArgs.Empty);

        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void BuildTransitions_without_validate_returns_transition_for_default_ctor_builder_without_guard()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        builder.Target(FlatState.B);

        var transition = builder.BuildTransitions().Should().ContainSingle().Subject;
        transition.Target.Should().Be(FlatState.B);
    }

    [Fact]
    public void BuildTransitions_without_validate_can_produce_shape_without_target()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();

        var transition = builder.BuildTransitions().Should().ContainSingle().Subject;
        transition.IsInternal.Should().BeFalse();
        transition.Target.Should().Be(default(FlatState));
    }
}

public class StateTriggerBuilderTests
{
    [Fact]
    public void When_Invoke_Target_work_without_trigger_payload_but_receive_context()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor>();
        builder
            .When(ctx => ctx.Counter > 1)
            .Target(_ => FlatState.C)
            .Invoke(ctx => ctx.Log.Add("x"));
        builder.Validate();

        var transition = builder.BuildTransitions()[0];

        transition.Guard!(new TestContext { Counter = 0 }, EmptyServiceProvider.Instance, TestTriggerArgs.From(99)).Should().BeFalse();
        transition.Guard!(new TestContext { Counter = 5 }, EmptyServiceProvider.Instance, TestTriggerArgs.From(99)).Should().BeTrue();

        var ctx = new TestContext();
        transition.SyncAction!(ctx, EmptyServiceProvider.Instance, TestTriggerArgs.From(123));
        ctx.Log.Should().Equal("x");

        transition.TargetSelector!.Should().NotBeNull();
        transition.TargetSelector!(ctx, EmptyServiceProvider.Instance, TestTriggerArgs.Empty).Should().Be(FlatState.C);
    }

    [Fact]
    public async Task ReactAsync_uses_actor_and_context_without_payload()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor>();
        builder.Stay().ReactAsync(async (_, ctx) =>
        {
            await Task.Yield();
            ctx.Counter *= 10;
        });
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        var ctx = new TestContext { Counter = 3 };
        await transition.ReactionAsync!(new TestActor(), ctx, EmptyServiceProvider.Instance, TestTriggerArgs.Empty);

        ctx.Counter.Should().Be(30);
    }
}
