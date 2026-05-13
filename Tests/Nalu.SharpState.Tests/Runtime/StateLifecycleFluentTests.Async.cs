using FluentAssertions;
using TriggerArgs = Nalu.SharpState.Tests.Runtime.TestTriggerArgs;

namespace Nalu.SharpState.Tests.Runtime;

/// <summary>Async lifecycle default-interface tests for <see cref="IStateLifecycleFluent{TFluent, TContext}"/>.</summary>
public partial class StateLifecycleFluentTests
{

    [Fact]
    public async Task WhenEnteredAsync_T1_resolves_services() => await RunWhenEnteredAsyncArity(1);

    [Fact]
    public async Task WhenEnteredAsync_T2_resolves_services() => await RunWhenEnteredAsyncArity(2);

    [Fact]
    public async Task WhenEnteredAsync_T3_resolves_services() => await RunWhenEnteredAsyncArity(3);

    [Fact]
    public async Task WhenEnteredAsync_T4_resolves_services() => await RunWhenEnteredAsyncArity(4);

    [Fact]
    public async Task WhenEnteredAsync_T5_resolves_services() => await RunWhenEnteredAsyncArity(5);

    [Fact]
    public async Task WhenEnteredAsync_T6_resolves_services() => await RunWhenEnteredAsyncArity(6);

    [Fact]
    public async Task WhenEnteredAsync_T7_resolves_services() => await RunWhenEnteredAsyncArity(7);

    [Fact]
    public async Task WhenEnteredAsync_T8_resolves_services() => await RunWhenEnteredAsyncArity(8);

    [Fact]
    public async Task WhenEnteredAsync_T9_resolves_services() => await RunWhenEnteredAsyncArity(9);

    [Fact]
    public async Task WhenEnteredAsync_T10_resolves_services() => await RunWhenEnteredAsyncArity(10);

    [Fact]
    public async Task WhenEnteredAsync_T11_resolves_services() => await RunWhenEnteredAsyncArity(11);

    [Fact]
    public async Task WhenEnteredAsync_T12_resolves_services() => await RunWhenEnteredAsyncArity(12);

    [Fact]
    public async Task WhenEnteredAsync_T13_resolves_services() => await RunWhenEnteredAsyncArity(13);

    [Fact]
    public async Task WhenEnteredAsync_T14_resolves_services() => await RunWhenEnteredAsyncArity(14);

    [Fact]
    public async Task WhenEnteredAsync_T15_resolves_services() => await RunWhenEnteredAsyncArity(15);

    [Fact]
    public async Task WhenEnteredAsync_T16_resolves_services() => await RunWhenEnteredAsyncArity(16);

    [Fact]
    public async Task WhenExitedAsync_T1_resolves_services() => await RunWhenExitedAsyncArity(1);

    [Fact]
    public async Task WhenExitedAsync_T2_resolves_services() => await RunWhenExitedAsyncArity(2);

    [Fact]
    public async Task WhenExitedAsync_T3_resolves_services() => await RunWhenExitedAsyncArity(3);

    [Fact]
    public async Task WhenExitedAsync_T4_resolves_services() => await RunWhenExitedAsyncArity(4);

    [Fact]
    public async Task WhenExitedAsync_T5_resolves_services() => await RunWhenExitedAsyncArity(5);

    [Fact]
    public async Task WhenExitedAsync_T6_resolves_services() => await RunWhenExitedAsyncArity(6);

    [Fact]
    public async Task WhenExitedAsync_T7_resolves_services() => await RunWhenExitedAsyncArity(7);

    [Fact]
    public async Task WhenExitedAsync_T8_resolves_services() => await RunWhenExitedAsyncArity(8);

    [Fact]
    public async Task WhenExitedAsync_T9_resolves_services() => await RunWhenExitedAsyncArity(9);

    [Fact]
    public async Task WhenExitedAsync_T10_resolves_services() => await RunWhenExitedAsyncArity(10);

    [Fact]
    public async Task WhenExitedAsync_T11_resolves_services() => await RunWhenExitedAsyncArity(11);

    [Fact]
    public async Task WhenExitedAsync_T12_resolves_services() => await RunWhenExitedAsyncArity(12);

    [Fact]
    public async Task WhenExitedAsync_T13_resolves_services() => await RunWhenExitedAsyncArity(13);

    [Fact]
    public async Task WhenExitedAsync_T14_resolves_services() => await RunWhenExitedAsyncArity(14);

    [Fact]
    public async Task WhenExitedAsync_T15_resolves_services() => await RunWhenExitedAsyncArity(15);

    [Fact]
    public async Task WhenExitedAsync_T16_resolves_services() => await RunWhenExitedAsyncArity(16);

    [Fact]
    public void WhenEnteredAsync_context_only_throws_on_null_action()
    {
        var cfg = new LifecycleFluentTestConfigurator();
        IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext> fluent = cfg;

        var act = () => fluent.WhenEnteredAsync(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("action");
    }

    [Fact]
    public void WhenExitedAsync_context_only_throws_on_null_action()
    {
        var cfg = new LifecycleFluentTestConfigurator();
        IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext> fluent = cfg;

        var act = () => fluent.WhenExitedAsync(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("action");
    }

    [Fact]
    public void WhenEnteredAsync_returns_same_fluent_instance()
    {
        var cfg = new LifecycleFluentTestConfigurator();
        IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext> fluent = cfg;

        var returned = fluent.WhenEnteredAsync(_ => ValueTask.CompletedTask);

        ReferenceEquals(returned, cfg).Should().BeTrue();
    }

    [Fact]
    public void WhenExitedAsync_returns_same_fluent_instance()
    {
        var cfg = new LifecycleFluentTestConfigurator();
        IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext> fluent = cfg;

        var returned = fluent.WhenExitedAsync(_ => ValueTask.CompletedTask);

        ReferenceEquals(returned, cfg).Should().BeTrue();
    }

    [Fact]
    public void WhenEnteredAsync_second_call_throws_invalid_operation_exception()
    {
        var cfg = new LifecycleFluentTestConfigurator();
        IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext> fluent = cfg;
        fluent.WhenEnteredAsync(_ => ValueTask.CompletedTask);

        var act = () => fluent.WhenEnteredAsync(_ => ValueTask.CompletedTask);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WhenEnteredAsync has already been set*");
    }

    [Fact]
    public void WhenExitedAsync_second_call_throws_invalid_operation_exception()
    {
        var cfg = new LifecycleFluentTestConfigurator();
        IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext> fluent = cfg;
        fluent.WhenExitedAsync(_ => ValueTask.CompletedTask);

        var act = () => fluent.WhenExitedAsync(_ => ValueTask.CompletedTask);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WhenExitedAsync has already been set*");
    }

    [Fact]
    public async Task WhenEnteredAsync_service_missing_wraps_in_ReactionFailedException_on_FireAsync()
    {
        var definition = BuildDefinition(map =>
        {
            map[FlatState.A]
                .On(FlatTrigger.Go,
                    TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.B));
            ((IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext>)map[FlatState.B])
                .WhenEnteredAsync<MissingService>((_, _) => ValueTask.CompletedTask);
        });

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            definition,
            FlatState.A,
            new TestContext(),
            new TestActor(),
            TestServiceProviders.EmptyResolver);

        var ex = await FluentActions.Awaiting(async () => await engine.FireAsync(FlatTrigger.Go, TriggerArgs.Empty))
            .Should().ThrowAsync<ReactionFailedException>();
        ex.Which.InnerException.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Match($"*'{typeof(MissingService).FullName}'*");
    }

    [Fact]
    public async Task WhenExitedAsync_service_missing_wraps_in_ReactionFailedException_on_FireAsync()
    {
        var definition = BuildDefinition(map =>
        {
            map[FlatState.B]
                .On(FlatTrigger.Go,
                    TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.C));
            ((IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext>)map[FlatState.B])
                .WhenExitedAsync<MissingService>((_, _) => ValueTask.CompletedTask);
        });

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            definition,
            FlatState.B,
            new TestContext(),
            new TestActor(),
            TestServiceProviders.EmptyResolver);

        var ex = await FluentActions.Awaiting(async () => await engine.FireAsync(FlatTrigger.Go, TriggerArgs.Empty))
            .Should().ThrowAsync<ReactionFailedException>();
        ex.Which.InnerException.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Match($"*'{typeof(MissingService).FullName}'*");
    }

    private static async Task RunWhenEnteredAsyncArity(int arity)
    {
        var (provider, instances) = CreateArityServices(arity);
        Action<IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext>, object[]> register = arity switch
        {

            1 => (cfg, inst) =>
                _ = cfg.WhenEnteredAsync<L1>((ctx, s1) =>
                {
                    AssertSameInstances(ctx, inst, s1);
                    return ValueTask.CompletedTask;
                }),
            2 => (cfg, inst) =>
                _ = cfg.WhenEnteredAsync<L1, L2>((ctx, s1, s2) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2);
                    return ValueTask.CompletedTask;
                }),
            3 => (cfg, inst) =>
                _ = cfg.WhenEnteredAsync<L1, L2, L3>((ctx, s1, s2, s3) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3);
                    return ValueTask.CompletedTask;
                }),
            4 => (cfg, inst) =>
                _ = cfg.WhenEnteredAsync<L1, L2, L3, L4>((ctx, s1, s2, s3, s4) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4);
                    return ValueTask.CompletedTask;
                }),
            5 => (cfg, inst) =>
                _ = cfg.WhenEnteredAsync<L1, L2, L3, L4, L5>((ctx, s1, s2, s3, s4, s5) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5);
                    return ValueTask.CompletedTask;
                }),
            6 => (cfg, inst) =>
                _ = cfg.WhenEnteredAsync<L1, L2, L3, L4, L5, L6>((ctx, s1, s2, s3, s4, s5, s6) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6);
                    return ValueTask.CompletedTask;
                }),
            7 => (cfg, inst) =>
                _ = cfg.WhenEnteredAsync<L1, L2, L3, L4, L5, L6, L7>((ctx, s1, s2, s3, s4, s5, s6, s7) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7);
                    return ValueTask.CompletedTask;
                }),
            8 => (cfg, inst) =>
                _ = cfg.WhenEnteredAsync<L1, L2, L3, L4, L5, L6, L7, L8>((ctx, s1, s2, s3, s4, s5, s6, s7, s8) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8);
                    return ValueTask.CompletedTask;
                }),
            9 => (cfg, inst) =>
                _ = cfg.WhenEnteredAsync<L1, L2, L3, L4, L5, L6, L7, L8, L9>((ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9);
                    return ValueTask.CompletedTask;
                }),
            10 => (cfg, inst) =>
                _ = cfg.WhenEnteredAsync<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10>((ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10);
                    return ValueTask.CompletedTask;
                }),
            11 => (cfg, inst) =>
                _ = cfg.WhenEnteredAsync<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11>((ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11);
                    return ValueTask.CompletedTask;
                }),
            12 => (cfg, inst) =>
                _ = cfg.WhenEnteredAsync<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12>((ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12);
                    return ValueTask.CompletedTask;
                }),
            13 => (cfg, inst) =>
                _ = cfg.WhenEnteredAsync<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12, L13>((ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13);
                    return ValueTask.CompletedTask;
                }),
            14 => (cfg, inst) =>
                _ = cfg.WhenEnteredAsync<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12, L13, L14>((ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14);
                    return ValueTask.CompletedTask;
                }),
            15 => (cfg, inst) =>
                _ = cfg.WhenEnteredAsync<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12, L13, L14, L15>((ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14, s15) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14, s15);
                    return ValueTask.CompletedTask;
                }),
            16 => (cfg, inst) =>
                _ = cfg.WhenEnteredAsync<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12, L13, L14, L15, L16>((ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14, s15, s16) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14, s15, s16);
                    return ValueTask.CompletedTask;
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(arity))
        };

        var definition = BuildDefinition(map =>
        {
            map[FlatState.A]
                .On(FlatTrigger.Go,
                    TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.B));
            register(map[FlatState.B], instances);
        });

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            definition,
            FlatState.A,
            new TestContext(),
            new TestActor(),
            new StateMachineStaticServiceProviderResolver(provider));

        await engine.FireAsync(FlatTrigger.Go, TriggerArgs.Empty);

        engine.CurrentState.Should().Be(FlatState.B);
        engine.Context.Log.Should().ContainSingle().Which.Should().Be("ok");
    }

    private static async Task RunWhenExitedAsyncArity(int arity)
    {
        var (provider, instances) = CreateArityServices(arity);
        Action<IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext>, object[]> register = arity switch
        {

            1 => (cfg, inst) =>
                _ = cfg.WhenExitedAsync<L1>((ctx, s1) =>
                {
                    AssertSameInstances(ctx, inst, s1);
                    return ValueTask.CompletedTask;
                }),
            2 => (cfg, inst) =>
                _ = cfg.WhenExitedAsync<L1, L2>((ctx, s1, s2) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2);
                    return ValueTask.CompletedTask;
                }),
            3 => (cfg, inst) =>
                _ = cfg.WhenExitedAsync<L1, L2, L3>((ctx, s1, s2, s3) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3);
                    return ValueTask.CompletedTask;
                }),
            4 => (cfg, inst) =>
                _ = cfg.WhenExitedAsync<L1, L2, L3, L4>((ctx, s1, s2, s3, s4) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4);
                    return ValueTask.CompletedTask;
                }),
            5 => (cfg, inst) =>
                _ = cfg.WhenExitedAsync<L1, L2, L3, L4, L5>((ctx, s1, s2, s3, s4, s5) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5);
                    return ValueTask.CompletedTask;
                }),
            6 => (cfg, inst) =>
                _ = cfg.WhenExitedAsync<L1, L2, L3, L4, L5, L6>((ctx, s1, s2, s3, s4, s5, s6) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6);
                    return ValueTask.CompletedTask;
                }),
            7 => (cfg, inst) =>
                _ = cfg.WhenExitedAsync<L1, L2, L3, L4, L5, L6, L7>((ctx, s1, s2, s3, s4, s5, s6, s7) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7);
                    return ValueTask.CompletedTask;
                }),
            8 => (cfg, inst) =>
                _ = cfg.WhenExitedAsync<L1, L2, L3, L4, L5, L6, L7, L8>((ctx, s1, s2, s3, s4, s5, s6, s7, s8) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8);
                    return ValueTask.CompletedTask;
                }),
            9 => (cfg, inst) =>
                _ = cfg.WhenExitedAsync<L1, L2, L3, L4, L5, L6, L7, L8, L9>((ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9);
                    return ValueTask.CompletedTask;
                }),
            10 => (cfg, inst) =>
                _ = cfg.WhenExitedAsync<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10>((ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10);
                    return ValueTask.CompletedTask;
                }),
            11 => (cfg, inst) =>
                _ = cfg.WhenExitedAsync<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11>((ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11);
                    return ValueTask.CompletedTask;
                }),
            12 => (cfg, inst) =>
                _ = cfg.WhenExitedAsync<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12>((ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12);
                    return ValueTask.CompletedTask;
                }),
            13 => (cfg, inst) =>
                _ = cfg.WhenExitedAsync<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12, L13>((ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13);
                    return ValueTask.CompletedTask;
                }),
            14 => (cfg, inst) =>
                _ = cfg.WhenExitedAsync<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12, L13, L14>((ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14);
                    return ValueTask.CompletedTask;
                }),
            15 => (cfg, inst) =>
                _ = cfg.WhenExitedAsync<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12, L13, L14, L15>((ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14, s15) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14, s15);
                    return ValueTask.CompletedTask;
                }),
            16 => (cfg, inst) =>
                _ = cfg.WhenExitedAsync<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12, L13, L14, L15, L16>((ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14, s15, s16) =>
                {
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14, s15, s16);
                    return ValueTask.CompletedTask;
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(arity))
        };

        var definition = BuildDefinition(map =>
        {
            map[FlatState.B]
                .On(FlatTrigger.Go,
                    TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.C));
            register(map[FlatState.B], instances);
        });

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            definition,
            FlatState.B,
            new TestContext(),
            new TestActor(),
            new StateMachineStaticServiceProviderResolver(provider));

        await engine.FireAsync(FlatTrigger.Go, TriggerArgs.Empty);

        engine.CurrentState.Should().Be(FlatState.C);
        engine.Context.Log.Should().ContainSingle().Which.Should().Be("ok");
    }
}
