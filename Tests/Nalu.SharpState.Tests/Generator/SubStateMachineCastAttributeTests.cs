using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace Nalu.SharpState.Tests.Generator;

public class SubStateMachineCastAttributeTests
{
    [Fact]
    public void SubStateMachine_cast_enum_in_argument_uses_ExtractEnumName_when_syntax_tail_missing()
    {
        var source = """
        using Nalu.SharpState;

        namespace Sample;

        public class Ctx { }

        [StateMachineDefinition(typeof(Ctx))]
        public static partial class CastParentMachine
        {
            [StateTriggerDefinition] static partial void Go();

            [StateDefinition(Initial = true)]
            private static IStateConfiguration A { get; } = ConfigureState();

            [StateDefinition]
            private static IStateConfiguration B { get; } = ConfigureState();

            // CastExpression: no identifier tail; parent is resolved from constructor enum argument.
            [SubStateMachine((State)1)]
            private partial class Inner
            {
                [StateDefinition(Initial = true)]
                private static IStateConfiguration InnerInitial { get; } = ConfigureState();
            }
        }
        """;

        _ = GeneratorDriverHelper.RunGenerator(source, out var compilation);
        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
    }

    [Fact]
    public void SubStateMachine_positional_enum_member_uses_ExtractEnumName()
    {
        var source = """
        using Nalu.SharpState;

        namespace Sample;

        public class Ctx { }

        [StateMachineDefinition(typeof(Ctx))]
        public static partial class EnumCtorMachine
        {
            [StateTriggerDefinition] static partial void Go();

            [StateDefinition(Initial = true)]
            private static IStateConfiguration A { get; } = ConfigureState();

            [StateDefinition]
            private static IStateConfiguration B { get; } = ConfigureState();

            [SubStateMachine(State.B)]
            private partial class Inner
            {
                [StateDefinition(Initial = true)]
                private static IStateConfiguration InnerInitial { get; } = ConfigureState();
            }
        }
        """;

        _ = GeneratorDriverHelper.RunGenerator(source, out var compilation);
        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
    }
}
