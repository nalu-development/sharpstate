using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class StateTriggerBuilderTests
{
    [Fact]
    public void Validate_requires_Target_or_Stay()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor>();
        var act = builder.Validate;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Target*Stay*");
    }

    [Fact]
    public void Validate_rejects_Target_and_Stay_together()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor> targetPhase = builder;
        targetPhase.Target(FlatState.B);
        ((ISyncStateTriggerBuilder<TestContext, FlatState, TestActor>)builder).Stay();
        var act = () => builder.Validate();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void BuildTransitions_produces_single_transition_with_target()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor> sync = builder;
        sync.Target(FlatState.B);
        builder.Validate();

        var list = builder.BuildTransitions();

        list.Should().ContainSingle();
        var transition = list[0];
        transition.IsInternal.Should().BeFalse();
        transition.Target.Should().Be(FlatState.B);
    }

    [Fact]
    public void BuildTransitions_produces_single_transition_with_dynamic_target()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor, int>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor, int> targetPhase = builder;
        targetPhase.Target((ctx, step) => ctx.Counter == step ? FlatState.B : FlatState.C);
        builder.Validate();

        var transition = builder.BuildTransitions()[0];

        transition.TargetSelector.Should().NotBeNull();
        transition.TargetSelector!(new TestContext { Counter = 3 }, TriggerArgs.From(3)).Should().Be(FlatState.B);
        transition.TargetSelector!(new TestContext { Counter = 1 }, TriggerArgs.From(3)).Should().Be(FlatState.C);
        var act = () => _ = transition.Target;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*dispatch time*");
    }

    [Fact]
    public void BuildTransitions_produces_internal_transition_on_Stay()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor> sync = builder;
        sync.Stay();
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        transition.IsInternal.Should().BeTrue();
        var act = () => _ = transition.Target;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void One_arg_builder_unpacks_argument()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor, string>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor, string> sync = builder;
        sync
            .When((_, arg) => arg == "ok")
            .Target(FlatState.B)
            .Invoke((ctx, arg) => ctx.LastArg = arg);
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        var ctx = new TestContext();
        transition.Guard!(ctx, TriggerArgs.From("bad")).Should().BeFalse();
        transition.Guard!(ctx, TriggerArgs.From("ok")).Should().BeTrue();
        transition.SyncAction!(ctx, TriggerArgs.From("ok"));
        ctx.LastArg.Should().Be("ok");
    }

    [Fact]
    public void Two_arg_builder_unpacks_both_arguments()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor, string, int>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor, string, int> sync = builder;
        sync
            .When((_, s, i) => s.Length == i)
            .Target(FlatState.B)
            .Invoke((ctx, s, i) => ctx.Log.Add($"{s}:{i}"));
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        var ctx = new TestContext();
        transition.Guard!(ctx, TriggerArgs.From("hi", 2)).Should().BeTrue();
        transition.Guard!(ctx, TriggerArgs.From("hi", 3)).Should().BeFalse();
        transition.SyncAction!(ctx, TriggerArgs.From("hi", 2));
        ctx.Log.Should().Equal("hi:2");
    }

    [Fact]
    public void Repeated_When_and_Invoke_registrations_compose_in_definition_order()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor, string>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor, string> targetPhase = builder;
        targetPhase
            .When((ctx, arg) =>
            {
                ctx.Log.Add("guard:1");
                return arg.StartsWith("o", StringComparison.Ordinal);
            })
            .When((ctx, arg) =>
            {
                ctx.Log.Add("guard:2");
                return arg.EndsWith("k", StringComparison.Ordinal);
            })
            .Target(FlatState.B)
            .Invoke((ctx, arg) => ctx.Log.Add("action:1:" + arg))
            .Invoke((ctx, arg) => ctx.Log.Add("action:2:" + arg));
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        var ctx = new TestContext();

        transition.Guard!(ctx, TriggerArgs.From("ok")).Should().BeTrue();
        ctx.Log.Should().Equal("guard:1", "guard:2");

        ctx.Log.Clear();
        transition.Guard!(ctx, TriggerArgs.From("no")).Should().BeFalse();
        ctx.Log.Should().Equal("guard:1");

        ctx.Log.Clear();
        transition.SyncAction!(ctx, TriggerArgs.From("ok"));
        ctx.Log.Should().Equal("action:1:ok", "action:2:ok");
    }

    [Fact]
    public void Repeated_When_registrations_preserve_guard_labels_in_definition_order()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor, string>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor, string> targetPhase = builder;
        targetPhase
            .When((_, arg) => arg.StartsWith("o", StringComparison.Ordinal), "Starts with o")
            .When((_, arg) => arg.EndsWith("k", StringComparison.Ordinal))
            .Target(FlatState.B);
        builder.Validate();

        var transition = builder.BuildTransitions()[0];

        transition.GuardLabels.Should().Equal("Starts with o");
    }

    [Fact]
    public async Task ReactAsync_stores_background_reaction()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor, int>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor, int> sync = builder;
        sync
            .Target(FlatState.B)
            .ReactAsync(async (_, ctx, i) =>
            {
                await Task.Yield();
                ctx.Counter += i;
            });
        builder.Validate();

        var actor = new TestActor();
        var transition = builder.BuildTransitions()[0];
        transition.ReactionAsync.Should().NotBeNull();
        var ctx = new TestContext { Counter = 10 };
        await transition.ReactionAsync!(actor, ctx, TriggerArgs.From(5));
        ctx.Counter.Should().Be(15);
    }

    [Fact]
    public async Task Repeated_ReactAsync_registrations_compose_in_definition_order()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor, int>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor, int> targetPhase = builder;
        targetPhase
            .Target(FlatState.B)
            .ReactAsync(async (_, ctx, value) =>
            {
                await Task.Yield();
                ctx.Log.Add("reaction:1:" + value);
            })
            .ReactAsync(async (_, ctx, value) =>
            {
                await Task.Yield();
                ctx.Log.Add("reaction:2:" + value);
            });
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        var ctx = new TestContext();

        await transition.ReactionAsync!(new TestActor(), ctx, TriggerArgs.From(7));
        ctx.Log.Should().Equal("reaction:1:7", "reaction:2:7");
    }

    [Fact]
    public void Ignore_is_syntax_sugar_for_internal_transition()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor> sync = builder;
        sync.Ignore();
        builder.Validate();

        builder.BuildTransitions()[0].IsInternal.Should().BeTrue();
    }

    [Fact]
    public void Zero_arg_dynamic_Target_uses_context_only_selector()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor> sync = builder;
        sync
            .When(_ => true)
            .Target(ctx => ctx.Counter < 0 ? FlatState.C : FlatState.B);
        builder.Validate();

        var t = builder.BuildTransitions()[0];
        t.TargetSelector.Should().NotBeNull();
        t.TargetSelector!(new TestContext { Counter = 1 }, TriggerArgs.Empty).Should().Be(FlatState.B);
    }

    [Fact]
    public async Task Zero_arg_ReactAsync_without_args()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor> sync = builder;
        sync.Target(FlatState.B).ReactAsync(async (_, ctx) =>
        {
            await Task.Yield();
            ctx.Log.Add("r");
        });
        builder.Validate();

        var t = builder.BuildTransitions()[0];
        var ctx = new TestContext();
        await t.ReactionAsync!(new TestActor(), ctx, TriggerArgs.Empty);
        ctx.Log.Should().Equal("r");
    }

    [Fact]
    public async Task Three_arg_builder_Target_and_reaction_unpack_three_arguments()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor, int, int, int>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor, int, int, int> sync = builder;
        sync
            .When((_, a, b, c) => a + b == c)
            .Target(
                (_, _, _, c) => c > 5 ? FlatState.C : FlatState.B)
            .ReactAsync(async (_, ctx, a, b, c) =>
            {
                await Task.Yield();
                ctx.Log.Add($"{a}{b}{c}");
            });
        builder.Validate();

        var t = builder.BuildTransitions()[0];
        var ctx = new TestContext();
        t.Guard!(ctx, TriggerArgs.From(1, 2, 3)).Should().BeTrue();
        await t.ReactionAsync!(new TestActor(), ctx, TriggerArgs.From(1, 2, 4));
        ctx.Log.Should().Equal("124");
    }

    [Fact]
    public void Four_arg_builder_Invoke_unrolls_arguments()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor, int, int, int, int>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor, int, int, int, int> sync = builder;
        sync
            .When((_, a, b, c, d) => a * b * c * d == 0)
            .Target(FlatState.B)
            .Invoke((ctx, a, b, c, d) => ctx.Log.Add($"args:{a}{b}{c}{d}"));
        builder.Validate();

        var t = builder.BuildTransitions()[0];
        var ctx = new TestContext();
        t.SyncAction!(ctx, TriggerArgs.From(1, 2, 0, 4));
        ctx.Log.Should().Equal("args:1204");
    }

    [Fact]
    public void Zero_arg_builder_Invoke_with_Action_of_Context()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor> trigger = builder;
        ISyncStateTransitionBuilder<TestContext, FlatState, TestActor> chain = trigger
            .When(_ => true)
            .Target(FlatState.B);
        chain.Invoke(c => c.Log.Add("x"));
        builder.Validate();
        var ctx = new TestContext();
        builder.BuildTransitions()[0].SyncAction!(ctx, TriggerArgs.Empty);
        ctx.Log.Should().Equal("x");
    }

    [Fact]
    public void One_arg_Ignore_does_not_return_builder()
    {
        var builder = new StateTriggerBuilder<TestContext, FlatState, TestActor, int>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor, int> t = builder;
        t.Ignore();
        builder.Validate();
        builder.BuildTransitions()[0].IsInternal.Should().BeTrue();
    }

    [Fact]
    public async Task Two_arg_dynamic_target_Stay_Ignore_ReactAsync()
    {
        var b = new StateTriggerBuilder<TestContext, FlatState, TestActor, int, int>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor, int, int> t = b;
        t.Target((_, a, x) => a < x ? FlatState.B : FlatState.C);
        b.Validate();
        b.BuildTransitions()[0].TargetSelector.Should().NotBeNull();

        b = new();
        t = b;
        t.Stay();
        b.Validate();
        b.BuildTransitions()[0].IsInternal.Should().BeTrue();

        b = new();
        t = b;
        t.Ignore();
        b.Validate();
        b.BuildTransitions()[0].IsInternal.Should().BeTrue();

        b = new();
        t = b;
        t.Target(FlatState.B).ReactAsync(async (_, c, a, x) =>
        {
            await Task.Yield();
            c.Log.Add($"{a}{x}");
        });
        b.Validate();
        var ctx = new TestContext();
        await b.BuildTransitions()[0].ReactionAsync!(new TestActor(), ctx, TriggerArgs.From(1, 2));
        ctx.Log.Should().Equal("12");
    }

    [Fact]
    public void Three_arg_constant_target_Stay_Ignore_Invoke()
    {
        var b = new StateTriggerBuilder<TestContext, FlatState, TestActor, int, int, int>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor, int, int, int> t = b;
        t.Target(FlatState.A);
        b.Validate();

        b = new();
        t = b;
        t.Stay();
        b.Validate();

        b = new();
        t = b;
        t.Ignore();
        b.Validate();

        b = new();
        t = b;
        t.When((_, a, b2, c) => a + b2 == c).Target(FlatState.B)
            .Invoke((ctx, a, b2, c) => ctx.Log.Add($"{a}{b2}{c}"));
        b.Validate();
        var ctx = new TestContext();
        b.BuildTransitions()[0].SyncAction!(ctx, TriggerArgs.From(1, 2, 3));
        ctx.Log.Should().Equal("123");
    }

    [Fact]
    public async Task Four_arg_dynamic_target_Stay_Ignore_ReactAsync()
    {
        var b = new StateTriggerBuilder<TestContext, FlatState, TestActor, int, int, int, int>();
        ISyncStateTriggerBuilder<TestContext, FlatState, TestActor, int, int, int, int> t = b;
        t.Target(
            (ctx, a, _, _, _) => ctx.Counter == a ? FlatState.A : FlatState.B);
        b.Validate();
        b.BuildTransitions()[0].TargetSelector!(new TestContext { Counter = 1 }, TriggerArgs.From(1, 0, 0, 0))
            .Should().Be(FlatState.A);

        b = new();
        t = b;
        t.Stay();
        b.Validate();

        b = new();
        t = b;
        t.Ignore();
        b.Validate();

        b = new();
        t = b;
        t.Target(FlatState.B)
            .ReactAsync(async (_, ctx, a, b0, c, d) =>
            {
                await Task.Yield();
                ctx.Log.Add($"{a}{b0}{c}{d}");
            });
        b.Validate();
        var ctx = new TestContext();
        await b.BuildTransitions()[0].ReactionAsync!(new TestActor(), ctx, TriggerArgs.From(4, 3, 2, 1));
        ctx.Log.Should().Equal("4321");
    }
}
