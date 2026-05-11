using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class StateTriggerBuilderTests
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
}
