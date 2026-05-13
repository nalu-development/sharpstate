using FluentAssertions;

namespace Nalu.SharpState.Tests.Generator;

public class EmitterCoverageTests
{
    [Fact]
    public void NonStaticMachineEmitsImplementingPartialsAndPrivateConstructorThatCallsTriggers()
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
        text.Should().Contain("public static partial void Pair(int a, int b)");
        text.Should().Contain("_ = a;");
        text.Should().Contain("_ = b;");
        text.Should().Contain("Pair(default, default);");
    }

    [Fact]
    public void MachineWithoutTriggersDoesNotEmitTriggerPartialStubsOrReferenceConstructor()
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
    public void StaticMachineEmitsStaticConstructorThatCallsTriggersAfterImplementingPartials()
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
        text.Should().Contain("public static partial void Pair(int a, int b)");
        text.Should().Contain("_ = a;");
        text.Should().Contain("_ = b;");
        text.Should().Contain("Pair(default, default);");
    }

    [Fact]
    public void ParameterlessTriggerEmitsStaticTriggerArgsFactoryAndParameterlessBuilder()
    {
        var source = """
            using Nalu.SharpState;

            namespace Sample;

            public class Ctx { }

            [StateMachineDefinition(typeof(Ctx))]
            public static partial class M
            {
                [StateTriggerDefinition] public static partial void WithPair(int deviceId);

                [StateTriggerDefinition] public static partial void NoArgs();

                [StateDefinition(Initial = true)]
                public static IStateConfiguration A { get; } = null!;
            }
            """;

        var result = GeneratorDriverHelper.RunGenerator(source, out _);
        var text = string.Join("\n", result.GeneratedTrees.Select(t => t.ToString()));
        text.Should().Contain("public static TriggerArgs ForNoArgs()");
        text.Should().Contain("IStateTriggerBuilder<");
        text.Should().Contain("IStateTriggerArgsBuilder<");
        text.Should().NotContain("struct NoArgsArgs");
        text.Should().Contain("TriggerArgs.ForNoArgs()");
        text.Should().Contain("public TriggerArgs(WithPairArgs value)");
        text.Should().Contain("StateTriggerBuilder<");
    }
}
