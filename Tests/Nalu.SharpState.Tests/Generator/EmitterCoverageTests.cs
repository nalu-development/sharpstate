using FluentAssertions;

namespace Nalu.SharpState.Tests.Generator;

public class EmitterCoverageTests
{
    [Fact]
    public void Non_static_machine_emits_private_constructor_for_trigger_reference_block()
    {
        var source = """
            using Nalu.SharpState;

            namespace Sample;

            public class Ctx { }

            [StateMachineDefinition(typeof(Ctx))]
            public partial class InstanceMachine
            {
                [StateTriggerDefinition] public static partial void Pair(int a, int b);

                [StateDefinition(Initial = true)]
                public static IStateConfiguration A { get; } = null!;
            }
            """;

        var result = GeneratorDriverHelper.RunGenerator(source, out _);
        var text = string.Join("\n", result.GeneratedTrees.Select(t => t.ToString()));
        text.Should().Contain("private InstanceMachine(");
        text.Should().Contain("Pair(default, default);");
    }

    [Fact]
    public void Machine_without_triggers_does_not_emit_trigger_reference_constructor()
    {
        var source = """
            using Nalu.SharpState;

            namespace Sample;

            public class Ctx { }

            [StateMachineDefinition(typeof(Ctx))]
            public static partial class NoTriggers
            {
                [StateDefinition(Initial = true)]
                public static IStateConfiguration Only { get; } = null!;
            }
            """;

        var result = GeneratorDriverHelper.RunGenerator(source, out _);
        var text = string.Join("\n", result.GeneratedTrees.Select(t => t.ToString()));
        text.Should().NotContain("private NoTriggers(");
        text.Should().NotContain("static NoTriggers(");
    }

    [Fact]
    public void Static_machine_emits_static_constructor_for_trigger_reference_block()
    {
        var source = """
            using Nalu.SharpState;

            namespace Sample;

            public class Ctx { }

            [StateMachineDefinition(typeof(Ctx))]
            public static partial class StaticPair
            {
                [StateTriggerDefinition] public static partial void Pair(int a, int b);

                [StateDefinition(Initial = true)]
                public static IStateConfiguration A { get; } = null!;
            }
            """;

        var result = GeneratorDriverHelper.RunGenerator(source, out _);
        var text = string.Join("\n", result.GeneratedTrees.Select(t => t.ToString()));
        text.Should().Contain("static StaticPair(");
        text.Should().Contain("Pair(default, default);");
    }
}
