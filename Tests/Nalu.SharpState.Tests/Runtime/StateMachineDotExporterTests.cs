using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class StateMachineDotExporterTests
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
                FlatState.B,
                FlatState.C));

        var forDef = new InternalEnumMap<FlatState, IStateConfiguration<TestContext, FlatState, FlatTrigger, TestActor>>();
        foreach (var kvp in map)
        {
            forDef[kvp.Key] = kvp.Value;
        }

        return new StateMachineDefinition<TestContext, FlatState, FlatTrigger, TestActor>(forDef);
    }

    [Fact]
    public void ToDot_dynamic_target_with_hint_states_emits_edges_to_states_not_placeholder()
    {
        var definition = BuildFlatWithDynamicHints();
        var dot = StateMachineDotExporter.ToDot(definition, FlatState.A, "Flat");

        dot.Should().Contain("trigger_");
        dot.Should().Contain("-> state_1;");
        dot.Should().Contain("-> state_2;");
        dot.Should().NotContain("Dynamic target");
        dot.Should().NotContain("dynamic_target_");
    }

    [Fact]
    public void ToDot_dynamic_target_without_hints_keeps_placeholder_node()
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
        var dot = StateMachineDotExporter.ToDot(definition, FlatState.A, "Flat");

        dot.Should().Contain("shape=rectangle,label=\"Dynamic target\"");
        dot.Should().Contain("dynamic_target_");
    }
}
