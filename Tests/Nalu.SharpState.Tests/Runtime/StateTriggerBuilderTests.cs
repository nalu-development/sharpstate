using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class StateTriggerArgsBuilderTests
{
    [Fact]
    public void ValidateRequiresTransitionToOrStay()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();

        var act = builder.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TransitionTo*Stay*");
    }

    [Fact]
    public void BuildTransitionsProducesSingleTransitionWithTarget()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();

        builder.TransitionTo(FlatState.B);
        builder.Validate();

        var transition = builder.BuildTransitions().Should().ContainSingle().Subject;
        transition.IsInternal.Should().BeFalse();
        transition.Target.Should().Be(FlatState.B);
    }

    [Fact]
    public void BuilderUsesTypedPayloadForGuardsActionsAndDynamicTargets()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        builder
            .When((ctx, args) => ctx.Counter == args.Get<int>(0), "counter matches")
            .TransitionTo((_, args) => args.Get<string>(1) == "b" ? FlatState.B : FlatState.C)
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
    public async Task ReactAsyncStoresBackgroundReaction()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        builder.TransitionTo(FlatState.B)
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
    public void IgnoreIsSyntaxSugarForInternalTransition()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();

        builder.Ignore();
        builder.Validate();

        builder.BuildTransitions()[0].IsInternal.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidateThrowsWhenTargetAndStayAreBothSet(bool targetFirst)
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        if (targetFirst)
        {
            builder.TransitionTo(FlatState.B);
            builder.Stay();
        }
        else
        {
            builder.Stay();
            builder.TransitionTo(FlatState.B);
        }

        var act = builder.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TransitionTo*Stay*");
    }

    [Fact]
    public void ConstructorGetArgsThrowsOnNull()
    {
        var act = () => new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("getArgs");
    }

    [Fact]
    public void TransitionToSelectorThrowsOnNullDelegate()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();

        var act = () => builder.TransitionTo(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("targetSelector");
    }

    [Fact]
    public void WhenThrowsOnNullDelegate()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();

        var act = () => builder.When(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("guard");
    }

    [Fact]
    public void InvokeThrowsOnNullDelegate()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();

        var act = () => builder.Invoke(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("action");
    }

    [Fact]
    public void ReactAsyncThrowsOnNullDelegate()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();

        var act = () => builder.ReactAsync(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("action");
    }

    [Fact]
    public void WhenMultipleGuardsAreCombinedWithAndShortCircuitsAndLabelsSkipNulls()
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
            .TransitionTo(FlatState.B);
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        var context = new TestContext();
        transition.Guard!(context, EmptyServiceProvider.Instance, TestTriggerArgs.Empty).Should().BeFalse();
        secondEvaluated.Should().BeFalse();
        transition.GuardLabels.Should().Equal("first", "last");
    }

    [Fact]
    public void MultipleWhenTrueAndTrueRunsBothGuards()
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
            .TransitionTo(FlatState.B);
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        transition.Guard!(new TestContext(), EmptyServiceProvider.Instance, TestTriggerArgs.Empty).Should().BeTrue();
        calls.Should().Be(2);
    }

    [Fact]
    public void InvokeMultipleActionsExecuteInDeclarationOrder()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        builder.TransitionTo(FlatState.B)
            .Invoke((ctx, _) => ctx.Log.Add("1"))
            .Invoke((ctx, _) => ctx.Log.Add("2"));
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        var context = new TestContext();
        transition.SyncAction!(context, EmptyServiceProvider.Instance, TestTriggerArgs.Empty);

        context.Log.Should().Equal("1", "2");
    }

    [Fact]
    public async Task ReactAsyncMultipleReactionsExecuteInDeclarationOrder()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        builder.TransitionTo(FlatState.B)
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
    public void TransitionToDynamicHintsAreStoredWhenProvided()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        builder.TransitionTo((_, _) => FlatState.B, (FlatState.C, "see-c"));
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        transition.DynamicTargetHints.Should().NotBeNull();
        transition.DynamicTargetHints!.Should().ContainSingle()
            .Which.Should().Be((FlatState.C, "see-c"));
    }

    [Fact]
    public void TransitionToDynamicHintsAreNullWhenNotProvided()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        builder.TransitionTo((_, _) => FlatState.B);
        builder.Validate();

        builder.BuildTransitions()[0].DynamicTargetHints.Should().BeNull();
    }

    [Fact]
    public void TransitionTargetThrowsForInternalTransition()
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
    public void TransitionTargetThrowsForDynamicTransition()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        builder.TransitionTo((_, _) => FlatState.B);
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        var act = () => _ = transition.Target;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Dynamic*");
    }

    [Fact]
    public void DefaultConstructorCastFailsForMismatchedMachineAndTriggerArgTypes()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, int>();
        builder.When((_, _) => true).TransitionTo(FlatState.B);
        builder.Validate();

        var transition = builder.BuildTransitions()[0];
        var act = () => transition.Guard!(new TestContext(), EmptyServiceProvider.Instance, TestTriggerArgs.Empty);

        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void BuildTransitionsWithoutValidateReturnsTransitionForDefaultCtorBuilderWithoutGuard()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        builder.TransitionTo(FlatState.B);

        var transition = builder.BuildTransitions().Should().ContainSingle().Subject;
        transition.Target.Should().Be(FlatState.B);
    }

    [Fact]
    public void BuildTransitionsWithoutValidateCanProduceShapeWithoutTarget()
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
    public void WhenInvokeTransitionToWorkWithoutTriggerPayloadButReceiveContext()
    {
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor>();
        builder
            .When(ctx => ctx.Counter > 1)
            .TransitionTo(_ => FlatState.C)
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
    public async Task ReactAsyncUsesActorAndContextWithoutPayload()
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
