namespace Nalu.SharpState.Tests.Runtime;

[UsesVerify]
public class StateMachineExporterTests
{
    private static StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor> BuildFlatWithDynamicHints()
    {
        var map = new InternalEnumMap<FlatState, TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new();
        map[FlatState.B] = new();
        map[FlatState.C] = new();
        map[FlatState.A].On(
            FlatTrigger.Go,
            TestTransition.ToDynamicTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                (_, _, args) => args.Get<bool>(0) ? FlatState.B : FlatState.C,
                null,
                null,
                null,
                null,
                (FlatState.B, "Flag is true"),
                (FlatState.C, "Flag is false")));

        var forDef = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        return new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(forDef);
    }

    [Fact]
    public Task DotFlatDynamicHints()
    {
        var definition = BuildFlatWithDynamicHints();
        var dot = StateMachineExporter.ToDot(definition, FlatState.A, "Flat");
        return Verify(dot, ExporterSnapshotTestSettings.Create());
    }

    [Fact]
    public Task DotFlatDynamicWithoutHints()
    {
        var map = new InternalEnumMap<FlatState, TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new();
        map[FlatState.B] = new();
        map[FlatState.C] = new();
        map[FlatState.A].On(
            FlatTrigger.Go,
            TestTransition.ToDynamicTarget<TestContext, IServiceProvider, FlatState, TestActor>((_, _, _) => FlatState.B));

        var forDef = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        var definition = new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(forDef);
        var dot = StateMachineExporter.ToDot(definition, FlatState.A, "Flat");
        return Verify(dot, ExporterSnapshotTestSettings.Create());
    }

    [Fact]
    public Task MermaidFlatDynamicHints()
    {
        var definition = BuildFlatWithDynamicHints();
        var mermaid = StateMachineExporter.ToMermaid(definition, FlatState.A, "Flat");
        return Verify(mermaid, ExporterSnapshotTestSettings.Create());
    }

    [Fact]
    public Task MermaidFlatDynamicWithoutHints()
    {
        var map = new InternalEnumMap<FlatState, TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new();
        map[FlatState.B] = new();
        map[FlatState.C] = new();
        map[FlatState.A].On(
            FlatTrigger.Go,
            TestTransition.ToDynamicTarget<TestContext, IServiceProvider, FlatState, TestActor>((_, _, _) => FlatState.B));

        var forDef = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        var definition = new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(forDef);
        var mermaid = StateMachineExporter.ToMermaid(definition, FlatState.A, "Flat");
        return Verify(mermaid, ExporterSnapshotTestSettings.Create());
    }

    [Fact]
    public Task MermaidGuardsAndFallbacks()
    {
        var map = new InternalEnumMap<FlatState, TestStateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new();
        map[FlatState.B] = new();
        map[FlatState.C] = new();
        map[FlatState.A]
            .On(
                FlatTrigger.Go,
                TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                    FlatState.B,
                    (_, _, _) => true,
                    ["Named guard"]))
            .On(
                FlatTrigger.Go,
                TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.C))
            .On(
                FlatTrigger.Alt,
                TestTransition.ToTarget<TestContext, IServiceProvider, FlatState, TestActor>(
                    FlatState.B,
                    (_, _, _) => true));

        var forDef = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        var definition = new StateMachineDefinition<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>(forDef);
        var mermaid = StateMachineExporter.ToMermaid(definition, FlatState.A, "Flat");
        return Verify(mermaid, ExporterSnapshotTestSettings.Create());
    }

    [Fact]
    public Task MermaidCompositeRegionStay()
    {
        var map = new InternalEnumMap<HierState, TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>>();
        map[HierState.Idle] = new();
        map[HierState.Connected] = new();
        map[HierState.Authenticating] = new();
        map[HierState.Authenticated] = new();
        map[HierState.Outside] = new();
        map[HierState.Connected]
            .AsStateMachine(HierState.Authenticating)
            .On(
                HierTrigger.Message,
                TestTransition.Stay<TestContext, IServiceProvider, HierState, TestActor>());
        map[HierState.Authenticating].Parent(HierState.Connected);
        map[HierState.Authenticated].Parent(HierState.Connected);

        var forDef = new InternalEnumMap<HierState, IStateConfiguration<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        var definition = new StateMachineDefinition<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(forDef);
        var mermaid = StateMachineExporter.ToMermaid(definition, HierState.Idle, "Hier");
        return Verify(mermaid, ExporterSnapshotTestSettings.Create());
    }

    [Fact]
    public Task MermaidCoalescesMultipleRegionStayTriggers()
    {
        var map = new InternalEnumMap<HierState, TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>>();
        map[HierState.Idle] = new();
        map[HierState.Connected] = new();
        map[HierState.Authenticating] = new();
        map[HierState.Authenticated] = new();
        map[HierState.Outside] = new();
        map[HierState.Connected]
            .AsStateMachine(HierState.Authenticating)
            .On(
                HierTrigger.Message,
                TestTransition.Stay<TestContext, IServiceProvider, HierState, TestActor>())
            .On(
                HierTrigger.Disconnect,
                TestTransition.Stay<TestContext, IServiceProvider, HierState, TestActor>());
        map[HierState.Authenticating].Parent(HierState.Connected);
        map[HierState.Authenticated].Parent(HierState.Connected);

        var forDef = new InternalEnumMap<HierState, IStateConfiguration<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        var definition = new StateMachineDefinition<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(forDef);
        var mermaid = StateMachineExporter.ToMermaid(definition, HierState.Idle, "Hier");
        return Verify(mermaid, ExporterSnapshotTestSettings.Create());
    }

    [Fact]
    public Task MermaidRendersGuardedAndUnguardedRegionStays()
    {
        var map = new InternalEnumMap<HierState, TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>>();
        map[HierState.Idle] = new();
        map[HierState.Connected] = new();
        map[HierState.Authenticating] = new();
        map[HierState.Authenticated] = new();
        map[HierState.Outside] = new();
        map[HierState.Connected]
            .AsStateMachine(HierState.Authenticating)
            .On(
                HierTrigger.Message,
                TestTransition.Stay<TestContext, IServiceProvider, HierState, TestActor>(
                    guard: (_, _, _) => true,
                    guardLabels: ["Has message"]))
            .On(
                HierTrigger.Disconnect,
                TestTransition.Stay<TestContext, IServiceProvider, HierState, TestActor>());
        map[HierState.Authenticating].Parent(HierState.Connected);
        map[HierState.Authenticated].Parent(HierState.Connected);

        var forDef = new InternalEnumMap<HierState, IStateConfiguration<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        var definition = new StateMachineDefinition<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(forDef);
        var mermaid = StateMachineExporter.ToMermaid(definition, HierState.Idle, "Hier");
        return Verify(mermaid, ExporterSnapshotTestSettings.Create());
    }

    [Fact]
    public void MermaidLabelsUnguardedCompositeChoiceFallbackAsElse()
    {
        var map = new InternalEnumMap<HierState, TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>>();
        map[HierState.Idle] = new();
        map[HierState.Connected] = new();
        map[HierState.Authenticating] = new();
        map[HierState.Authenticated] = new();
        map[HierState.Outside] = new();
        map[HierState.Connected]
            .AsStateMachine(HierState.Authenticating)
            .On(
                HierTrigger.Message,
                TestTransition.ToTarget<TestContext, IServiceProvider, HierState, TestActor>(
                    HierState.Outside,
                    (_, _, _) => true,
                    ["Can leave"]))
            .On(
                HierTrigger.Message,
                TestTransition.Stay<TestContext, IServiceProvider, HierState, TestActor>());
        map[HierState.Authenticating].Parent(HierState.Connected);
        map[HierState.Authenticated].Parent(HierState.Connected);

        var forDef = new InternalEnumMap<HierState, IStateConfiguration<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        var definition = new StateMachineDefinition<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(forDef);
        var mermaid = StateMachineExporter.ToMermaid(definition, HierState.Idle, "Hier");

        Assert.Contains("choice_0 --> state_4 : [Can leave]", mermaid);
        Assert.Contains("choice_0 --> state_1 : [Else]", mermaid);
    }
}
