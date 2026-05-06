using Nalu.SharpState.Tests;

namespace Nalu.SharpState.Tests.Runtime;

[UsesVerify]
public class StateMachineExporterTests
{
    private static StateMachineDefinition<TestContext, FlatState, FlatTrigger, TestActor> BuildFlatWithDynamicHints()
    {
        var map = new InternalEnumMap<FlatState, TestStateConfigurator<TestContext, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new();
        map[FlatState.B] = new();
        map[FlatState.C] = new();
        map[FlatState.A].On(
            FlatTrigger.Go,
            TestTransition.ToDynamicTarget<TestContext, FlatState, TestActor>(
                (_, args) => args.Get<bool>(0) ? FlatState.B : FlatState.C,
                null,
                null,
                null,
                null,
                (FlatState.B, "Flag is true"),
                (FlatState.C, "Flag is false")));

        var forDef = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, FlatState, FlatTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        return new StateMachineDefinition<TestContext, FlatState, FlatTrigger, TestActor>(forDef);
    }

    [Fact]
    public Task Dot_flat_dynamic_hints()
    {
        var definition = BuildFlatWithDynamicHints();
        var dot = StateMachineExporter.ToDot(definition, FlatState.A, "Flat");
        return Verify(dot, ExporterSnapshotTestSettings.Create());
    }

    [Fact]
    public Task Dot_flat_dynamic_without_hints()
    {
        var map = new InternalEnumMap<FlatState, TestStateConfigurator<TestContext, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new();
        map[FlatState.B] = new();
        map[FlatState.C] = new();
        map[FlatState.A].On(
            FlatTrigger.Go,
            TestTransition.ToDynamicTarget<TestContext, FlatState, TestActor>((_, _) => FlatState.B));

        var forDef = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, FlatState, FlatTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        var definition = new StateMachineDefinition<TestContext, FlatState, FlatTrigger, TestActor>(forDef);
        var dot = StateMachineExporter.ToDot(definition, FlatState.A, "Flat");
        return Verify(dot, ExporterSnapshotTestSettings.Create());
    }

    [Fact]
    public Task Mermaid_flat_dynamic_hints()
    {
        var definition = BuildFlatWithDynamicHints();
        var mermaid = StateMachineExporter.ToMermaid(definition, FlatState.A, "Flat");
        return Verify(mermaid, ExporterSnapshotTestSettings.Create());
    }

    [Fact]
    public Task Mermaid_flat_dynamic_without_hints()
    {
        var map = new InternalEnumMap<FlatState, TestStateConfigurator<TestContext, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new();
        map[FlatState.B] = new();
        map[FlatState.C] = new();
        map[FlatState.A].On(
            FlatTrigger.Go,
            TestTransition.ToDynamicTarget<TestContext, FlatState, TestActor>((_, _) => FlatState.B));

        var forDef = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, FlatState, FlatTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        var definition = new StateMachineDefinition<TestContext, FlatState, FlatTrigger, TestActor>(forDef);
        var mermaid = StateMachineExporter.ToMermaid(definition, FlatState.A, "Flat");
        return Verify(mermaid, ExporterSnapshotTestSettings.Create());
    }

    [Fact]
    public Task Mermaid_guards_and_fallbacks()
    {
        var map = new InternalEnumMap<FlatState, TestStateConfigurator<TestContext, FlatState, FlatTrigger, TestActor>>();
        map[FlatState.A] = new();
        map[FlatState.B] = new();
        map[FlatState.C] = new();
        map[FlatState.A]
            .On(
                FlatTrigger.Go,
                TestTransition.ToTarget<TestContext, FlatState, TestActor>(
                    FlatState.B,
                    (_, _) => true,
                    ["Named guard"]))
            .On(
                FlatTrigger.Go,
                TestTransition.ToTarget<TestContext, FlatState, TestActor>(FlatState.C))
            .On(
                FlatTrigger.Alt,
                TestTransition.ToTarget<TestContext, FlatState, TestActor>(
                    FlatState.B,
                    (_, _) => true));

        var forDef = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, FlatState, FlatTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        var definition = new StateMachineDefinition<TestContext, FlatState, FlatTrigger, TestActor>(forDef);
        var mermaid = StateMachineExporter.ToMermaid(definition, FlatState.A, "Flat");
        return Verify(mermaid, ExporterSnapshotTestSettings.Create());
    }

    [Fact]
    public Task Mermaid_composite_region_stay()
    {
        var map = new InternalEnumMap<HierState, TestStateConfigurator<TestContext, HierState, HierTrigger, TestActor>>();
        map[HierState.Idle] = new();
        map[HierState.Connected] = new();
        map[HierState.Authenticating] = new();
        map[HierState.Authenticated] = new();
        map[HierState.Outside] = new();
        map[HierState.Connected]
            .AsStateMachine(HierState.Authenticating)
            .On(
                HierTrigger.Message,
                TestTransition.Stay<TestContext, HierState, TestActor>());
        map[HierState.Authenticating].Parent(HierState.Connected);
        map[HierState.Authenticated].Parent(HierState.Connected);

        var forDef = new InternalEnumMap<HierState, IStateConfiguration<TestContext, HierState, HierTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        var definition = new StateMachineDefinition<TestContext, HierState, HierTrigger, TestActor>(forDef);
        var mermaid = StateMachineExporter.ToMermaid(definition, HierState.Idle, "Hier");
        return Verify(mermaid, ExporterSnapshotTestSettings.Create());
    }

    [Fact]
    public Task Mermaid_coalesces_multiple_region_stay_triggers()
    {
        var map = new InternalEnumMap<HierState, TestStateConfigurator<TestContext, HierState, HierTrigger, TestActor>>();
        map[HierState.Idle] = new();
        map[HierState.Connected] = new();
        map[HierState.Authenticating] = new();
        map[HierState.Authenticated] = new();
        map[HierState.Outside] = new();
        map[HierState.Connected]
            .AsStateMachine(HierState.Authenticating)
            .On(
                HierTrigger.Message,
                TestTransition.Stay<TestContext, HierState, TestActor>())
            .On(
                HierTrigger.Disconnect,
                TestTransition.Stay<TestContext, HierState, TestActor>());
        map[HierState.Authenticating].Parent(HierState.Connected);
        map[HierState.Authenticated].Parent(HierState.Connected);

        var forDef = new InternalEnumMap<HierState, IStateConfiguration<TestContext, HierState, HierTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        var definition = new StateMachineDefinition<TestContext, HierState, HierTrigger, TestActor>(forDef);
        var mermaid = StateMachineExporter.ToMermaid(definition, HierState.Idle, "Hier");
        return Verify(mermaid, ExporterSnapshotTestSettings.Create());
    }

    [Fact]
    public Task Mermaid_renders_guarded_and_unguarded_region_stays()
    {
        var map = new InternalEnumMap<HierState, TestStateConfigurator<TestContext, HierState, HierTrigger, TestActor>>();
        map[HierState.Idle] = new();
        map[HierState.Connected] = new();
        map[HierState.Authenticating] = new();
        map[HierState.Authenticated] = new();
        map[HierState.Outside] = new();
        map[HierState.Connected]
            .AsStateMachine(HierState.Authenticating)
            .On(
                HierTrigger.Message,
                TestTransition.Stay<TestContext, HierState, TestActor>(
                    guard: (_, _) => true,
                    guardLabels: ["Has message"]))
            .On(
                HierTrigger.Disconnect,
                TestTransition.Stay<TestContext, HierState, TestActor>());
        map[HierState.Authenticating].Parent(HierState.Connected);
        map[HierState.Authenticated].Parent(HierState.Connected);

        var forDef = new InternalEnumMap<HierState, IStateConfiguration<TestContext, HierState, HierTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        var definition = new StateMachineDefinition<TestContext, HierState, HierTrigger, TestActor>(forDef);
        var mermaid = StateMachineExporter.ToMermaid(definition, HierState.Idle, "Hier");
        return Verify(mermaid, ExporterSnapshotTestSettings.Create());
    }

    [Fact]
    public void Mermaid_labels_unguarded_composite_choice_fallback_as_else()
    {
        var map = new InternalEnumMap<HierState, TestStateConfigurator<TestContext, HierState, HierTrigger, TestActor>>();
        map[HierState.Idle] = new();
        map[HierState.Connected] = new();
        map[HierState.Authenticating] = new();
        map[HierState.Authenticated] = new();
        map[HierState.Outside] = new();
        map[HierState.Connected]
            .AsStateMachine(HierState.Authenticating)
            .On(
                HierTrigger.Message,
                TestTransition.ToTarget<TestContext, HierState, TestActor>(
                    HierState.Outside,
                    (_, _) => true,
                    ["Can leave"]))
            .On(
                HierTrigger.Message,
                TestTransition.Stay<TestContext, HierState, TestActor>());
        map[HierState.Authenticating].Parent(HierState.Connected);
        map[HierState.Authenticated].Parent(HierState.Connected);

        var forDef = new InternalEnumMap<HierState, IStateConfiguration<TestContext, HierState, HierTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        var definition = new StateMachineDefinition<TestContext, HierState, HierTrigger, TestActor>(forDef);
        var mermaid = StateMachineExporter.ToMermaid(definition, HierState.Idle, "Hier");

        Assert.Contains("choice_0 --> state_4 : [Can leave]", mermaid);
        Assert.Contains("choice_0 --> state_1 : [Else]", mermaid);
    }
}
