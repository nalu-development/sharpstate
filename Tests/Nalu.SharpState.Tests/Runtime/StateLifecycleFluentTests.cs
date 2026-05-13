using FluentAssertions;
using TriggerArgs = Nalu.SharpState.Tests.Runtime.TestTriggerArgs;

namespace Nalu.SharpState.Tests.Runtime;

public partial class StateLifecycleFluentTests
{
    /// <summary>Distinct service types for DI arity tests (L1 alone … L1…L16).</summary>
    private sealed class L1;

    private sealed class L2;

    private sealed class L3;

    private sealed class L4;

    private sealed class L5;

    private sealed class L6;

    private sealed class L7;

    private sealed class L8;

    private sealed class L9;

    private sealed class L10;

    private sealed class L11;

    private sealed class L12;

    private sealed class L13;

    private sealed class L14;

    private sealed class L15;

    private sealed class L16;

    private static readonly Type[] _markTypes =
    [
        typeof(L1),
        typeof(L2),
        typeof(L3),
        typeof(L4),
        typeof(L5),
        typeof(L6),
        typeof(L7),
        typeof(L8),
        typeof(L9),
        typeof(L10),
        typeof(L11),
        typeof(L12),
        typeof(L13),
        typeof(L14),
        typeof(L15),
        typeof(L16)
    ];

    private sealed class MissingService;

    private sealed class DictionaryProvider(Dictionary<Type, object?> services) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            services.TryGetValue(serviceType, out var value) ? value : null;
    }

    private static (DictionaryProvider Provider, object[] Instances) CreateArityServices(int arity)
    {
        arity.Should().BeInRange(1, 16);
        var instances = new object[arity];
        var dict = new Dictionary<Type, object?>();
        for (var i = 0; i < arity; i++)
        {
            instances[i] = Activator.CreateInstance(_markTypes[i])!;
            dict[_markTypes[i]] = instances[i];
        }

        return (new DictionaryProvider(dict), instances);
    }

    private static void AssertSameInstances(TestContext ctx, object[] expected, params object[] actual)
    {
        actual.Should().HaveCount(expected.Length);
        for (var i = 0; i < actual.Length; i++)
        {
            ReferenceEquals(actual[i], expected[i]).Should().BeTrue();
        }

        ctx.Log.Add("ok");
    }

    private static StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor> BuildDefinition(
        Action<InternalEnumMap<FlatState, LifecycleFluentTestConfigurator>>? setup = null)
    {
        var map = new InternalEnumMap<FlatState, LifecycleFluentTestConfigurator>();
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
    public void WhenEnteringContextOnlyThrowsOnNullAction()
    {
        var cfg = new LifecycleFluentTestConfigurator();
        IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext> fluent = cfg;

        var act = () => fluent.WhenEntering(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("action");
    }

    [Fact]
    public void WhenExitingContextOnlyThrowsOnNullAction()
    {
        var cfg = new LifecycleFluentTestConfigurator();
        IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext> fluent = cfg;

        Action act = () => fluent.WhenExiting(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("action");
    }

    [Fact]
    public void WhenEnteringReturnsSameFluentInstance()
    {
        var cfg = new LifecycleFluentTestConfigurator();
        IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext> fluent = cfg;

        var returned = fluent.WhenEntering(_ => { });

        ReferenceEquals(returned, cfg).Should().BeTrue();
    }

    [Fact]
    public void WhenExitingReturnsSameFluentInstance()
    {
        var cfg = new LifecycleFluentTestConfigurator();
        IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext> fluent = cfg;

        var returned = fluent.WhenExiting(_ => { });

        ReferenceEquals(returned, cfg).Should().BeTrue();
    }

    [Fact]
    public void WhenEnteringT1ResolvesServices() => RunWhenEnteringArity(1);

    [Fact]
    public void WhenEnteringT2ResolvesServices() => RunWhenEnteringArity(2);

    [Fact]
    public void WhenEnteringT3ResolvesServices() => RunWhenEnteringArity(3);

    [Fact]
    public void WhenEnteringT4ResolvesServices() => RunWhenEnteringArity(4);

    [Fact]
    public void WhenEnteringT5ResolvesServices() => RunWhenEnteringArity(5);

    [Fact]
    public void WhenEnteringT6ResolvesServices() => RunWhenEnteringArity(6);

    [Fact]
    public void WhenEnteringT7ResolvesServices() => RunWhenEnteringArity(7);

    [Fact]
    public void WhenEnteringT8ResolvesServices() => RunWhenEnteringArity(8);

    [Fact]
    public void WhenEnteringT9ResolvesServices() => RunWhenEnteringArity(9);

    [Fact]
    public void WhenEnteringT10ResolvesServices() => RunWhenEnteringArity(10);

    [Fact]
    public void WhenEnteringT11ResolvesServices() => RunWhenEnteringArity(11);

    [Fact]
    public void WhenEnteringT12ResolvesServices() => RunWhenEnteringArity(12);

    [Fact]
    public void WhenEnteringT13ResolvesServices() => RunWhenEnteringArity(13);

    [Fact]
    public void WhenEnteringT14ResolvesServices() => RunWhenEnteringArity(14);

    [Fact]
    public void WhenEnteringT15ResolvesServices() => RunWhenEnteringArity(15);

    [Fact]
    public void WhenEnteringT16ResolvesServices() => RunWhenEnteringArity(16);

    [Fact]
    public void WhenExitingT1ResolvesServices() => RunWhenExitingArity(1);

    [Fact]
    public void WhenExitingT2ResolvesServices() => RunWhenExitingArity(2);

    [Fact]
    public void WhenExitingT3ResolvesServices() => RunWhenExitingArity(3);

    [Fact]
    public void WhenExitingT4ResolvesServices() => RunWhenExitingArity(4);

    [Fact]
    public void WhenExitingT5ResolvesServices() => RunWhenExitingArity(5);

    [Fact]
    public void WhenExitingT6ResolvesServices() => RunWhenExitingArity(6);

    [Fact]
    public void WhenExitingT7ResolvesServices() => RunWhenExitingArity(7);

    [Fact]
    public void WhenExitingT8ResolvesServices() => RunWhenExitingArity(8);

    [Fact]
    public void WhenExitingT9ResolvesServices() => RunWhenExitingArity(9);

    [Fact]
    public void WhenExitingT10ResolvesServices() => RunWhenExitingArity(10);

    [Fact]
    public void WhenExitingT11ResolvesServices() => RunWhenExitingArity(11);

    [Fact]
    public void WhenExitingT12ResolvesServices() => RunWhenExitingArity(12);

    [Fact]
    public void WhenExitingT13ResolvesServices() => RunWhenExitingArity(13);

    [Fact]
    public void WhenExitingT14ResolvesServices() => RunWhenExitingArity(14);

    [Fact]
    public void WhenExitingT15ResolvesServices() => RunWhenExitingArity(15);

    [Fact]
    public void WhenExitingT16ResolvesServices() => RunWhenExitingArity(16);

    private static void RunWhenEnteringArity(int arity)
    {
        var (provider, instances) = CreateArityServices(arity);
        Action<IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext>, object[]> register = arity switch
        {
            1 => (cfg, inst) => _ = cfg.WhenEntering<L1>((ctx, s1) => AssertSameInstances(ctx, inst, s1)),
            2 => (cfg, inst) => _ = cfg.WhenEntering<L1, L2>((ctx, s1, s2) => AssertSameInstances(ctx, inst, s1, s2)),
            3 => (cfg, inst) =>
                _ = cfg.WhenEntering<L1, L2, L3>((ctx, s1, s2, s3) => AssertSameInstances(ctx, inst, s1, s2, s3)),
            4 => (cfg, inst) =>
                _ = cfg.WhenEntering<L1, L2, L3, L4>((ctx, s1, s2, s3, s4) =>
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4)),
            5 => (cfg, inst) =>
                _ = cfg.WhenEntering<L1, L2, L3, L4, L5>((ctx, s1, s2, s3, s4, s5) =>
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5)),
            6 => (cfg, inst) =>
                _ = cfg.WhenEntering<L1, L2, L3, L4, L5, L6>((ctx, s1, s2, s3, s4, s5, s6) =>
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6)),
            7 => (cfg, inst) =>
                _ = cfg.WhenEntering<L1, L2, L3, L4, L5, L6, L7>((ctx, s1, s2, s3, s4, s5, s6, s7) =>
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7)),
            8 => (cfg, inst) =>
                _ = cfg.WhenEntering<L1, L2, L3, L4, L5, L6, L7, L8>((ctx, s1, s2, s3, s4, s5, s6, s7, s8) =>
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8)),
            9 => (cfg, inst) =>
                _ = cfg.WhenEntering<L1, L2, L3, L4, L5, L6, L7, L8, L9>(
                    (ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9) =>
                        AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9)),
            10 => (cfg, inst) =>
                _ = cfg.WhenEntering<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10>(
                    (ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10) =>
                        AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10)),
            11 => (cfg, inst) =>
                _ = cfg.WhenEntering<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11>(
                    (ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11) =>
                        AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11)),
            12 => (cfg, inst) =>
                _ = cfg.WhenEntering<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12>(
                    (ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12) =>
                        AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12)),
            13 => (cfg, inst) =>
                _ = cfg.WhenEntering<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12, L13>(
                    (ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13) =>
                        AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13)),
            14 => (cfg, inst) =>
                _ = cfg.WhenEntering<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12, L13, L14>(
                    (ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14) =>
                        AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13,
                            s14)),
            15 => (cfg, inst) =>
                _ = cfg.WhenEntering<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12, L13, L14, L15>(
                    (ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14, s15) =>
                        AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13,
                            s14, s15)),
            16 => (cfg, inst) =>
                _ = cfg.WhenEntering<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12, L13, L14, L15, L16>(
                    (ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14, s15, s16) =>
                        AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13,
                            s14, s15, s16)),
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

        engine.Fire(FlatTrigger.Go, TriggerArgs.Empty);

        engine.CurrentState.Should().Be(FlatState.B);
        engine.Context.Log.Should().ContainSingle().Which.Should().Be("ok");
    }

    private static void RunWhenExitingArity(int arity)
    {
        var (provider, instances) = CreateArityServices(arity);
        Action<IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext>, object[]> register = arity switch
        {
            1 => (cfg, inst) => _ = cfg.WhenExiting<L1>((ctx, s1) => AssertSameInstances(ctx, inst, s1)),
            2 => (cfg, inst) => _ = cfg.WhenExiting<L1, L2>((ctx, s1, s2) => AssertSameInstances(ctx, inst, s1, s2)),
            3 => (cfg, inst) =>
                _ = cfg.WhenExiting<L1, L2, L3>((ctx, s1, s2, s3) => AssertSameInstances(ctx, inst, s1, s2, s3)),
            4 => (cfg, inst) =>
                _ = cfg.WhenExiting<L1, L2, L3, L4>((ctx, s1, s2, s3, s4) =>
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4)),
            5 => (cfg, inst) =>
                _ = cfg.WhenExiting<L1, L2, L3, L4, L5>((ctx, s1, s2, s3, s4, s5) =>
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5)),
            6 => (cfg, inst) =>
                _ = cfg.WhenExiting<L1, L2, L3, L4, L5, L6>((ctx, s1, s2, s3, s4, s5, s6) =>
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6)),
            7 => (cfg, inst) =>
                _ = cfg.WhenExiting<L1, L2, L3, L4, L5, L6, L7>((ctx, s1, s2, s3, s4, s5, s6, s7) =>
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7)),
            8 => (cfg, inst) =>
                _ = cfg.WhenExiting<L1, L2, L3, L4, L5, L6, L7, L8>((ctx, s1, s2, s3, s4, s5, s6, s7, s8) =>
                    AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8)),
            9 => (cfg, inst) =>
                _ = cfg.WhenExiting<L1, L2, L3, L4, L5, L6, L7, L8, L9>(
                    (ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9) =>
                        AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9)),
            10 => (cfg, inst) =>
                _ = cfg.WhenExiting<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10>(
                    (ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10) =>
                        AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10)),
            11 => (cfg, inst) =>
                _ = cfg.WhenExiting<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11>(
                    (ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11) =>
                        AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11)),
            12 => (cfg, inst) =>
                _ = cfg.WhenExiting<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12>(
                    (ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12) =>
                        AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12)),
            13 => (cfg, inst) =>
                _ = cfg.WhenExiting<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12, L13>(
                    (ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13) =>
                        AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13)),
            14 => (cfg, inst) =>
                _ = cfg.WhenExiting<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12, L13, L14>(
                    (ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14) =>
                        AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13,
                            s14)),
            15 => (cfg, inst) =>
                _ = cfg.WhenExiting<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12, L13, L14, L15>(
                    (ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14, s15) =>
                        AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13,
                            s14, s15)),
            16 => (cfg, inst) =>
                _ = cfg.WhenExiting<L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11, L12, L13, L14, L15, L16>(
                    (ctx, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14, s15, s16) =>
                        AssertSameInstances(ctx, inst, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13,
                            s14, s15, s16)),
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

        engine.Fire(FlatTrigger.Go, TriggerArgs.Empty);

        engine.CurrentState.Should().Be(FlatState.C);
        engine.Context.Log.Should().ContainSingle().Which.Should().Be("ok");
    }

    [Fact]
    public void WhenEnteringServiceMissingThrowsInvalidOperationException()
    {
        var definition = BuildDefinition(map =>
        {
            map[FlatState.A]
                .On(FlatTrigger.Go,
                    TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.B));
            ((IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext>)map[FlatState.B])
                .WhenEntering<MissingService>((_, _) => { });
        });

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            definition,
            FlatState.A,
            new TestContext(),
            new TestActor(),
            TestServiceProviders.EmptyResolver);

        var act = () => engine.Fire(FlatTrigger.Go, TriggerArgs.Empty);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*'{typeof(MissingService).FullName}'*");
    }

    [Fact]
    public void WhenExitingServiceMissingThrowsInvalidOperationException()
    {
        var definition = BuildDefinition(map =>
        {
            map[FlatState.B]
                .On(FlatTrigger.Go,
                    TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.C));
            ((IStateLifecycleFluent<LifecycleFluentTestConfigurator, TestContext>)map[FlatState.B])
                .WhenExiting<MissingService>((_, _) => { });
        });

        var engine = new StateMachineEngine<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(
            definition,
            FlatState.B,
            new TestContext(),
            new TestActor(),
            TestServiceProviders.EmptyResolver);

        var act = () => engine.Fire(FlatTrigger.Go, TriggerArgs.Empty);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*'{typeof(MissingService).FullName}'*");
    }
}
