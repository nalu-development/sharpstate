using FluentAssertions;
using Microsoft.CodeAnalysis;
// ReSharper disable InconsistentNaming

namespace Nalu.SharpState.Tests.Generator;

public class DiagnosticTests
{
    private static IReadOnlyList<Diagnostic> GetDiagnostics(string source) => GeneratorDriverHelper.RunGenerator(source, out _).Diagnostics;

    private static IReadOnlyList<Diagnostic> GetCompilationErrors(string source)
    {
        GeneratorDriverHelper.RunGenerator(source, out var outputCompilation);
        return outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
    }

    [Fact]
    public void NSS001ReportedWhenClassIsNotPartial()
    {
        var source = """
        using Nalu.SharpState;

        namespace Sample;

        public class Ctx { }

        [StateMachineDefinition(typeof(Ctx))]
        public class NotPartial
        {
            [StateTriggerDefinition] static partial void Go();
            [StateDefinition(Initial = true)] private static IStateConfiguration A { get; } = ConfigureState();
        }
        """;
        GetDiagnostics(source).Should().Contain(d => d.Id == "NSS001");
    }

    [Fact]
    public void NSS002ReportedOnDuplicateTrigger()
    {
        var source = """
        using Nalu.SharpState;

        namespace Sample;

        public class Ctx { }

        [StateMachineDefinition(typeof(Ctx))]
        public static partial class DupTrigger
        {
            [StateTriggerDefinition] static partial void Go();
            [StateTriggerDefinition] static partial void Go(int x);

            [StateDefinition(Initial = true)] private static IStateConfiguration A { get; } = ConfigureState();
        }
        """;
        GetDiagnostics(source).Should().Contain(d => d.Id == "NSS002");
    }

    [Fact]
    public void NSS002ReportedWhenDuplicateTriggerNameOverloadsConflictWithEnumGeneration()
    {
        var source = """
        using Nalu.SharpState;

        namespace Sample;

        public class Ctx { }

        [StateMachineDefinition(typeof(Ctx))]
        public static partial class DupOverload
        {
            [StateTriggerDefinition] static partial void Connect(string reason);
            [StateTriggerDefinition] static partial void Connect(int connectId);

            [StateDefinition(Initial = true)] private static IStateConfiguration A { get; } = ConfigureState();
        }
        """;

        GetDiagnostics(source).Should().Contain(d => d.Id == "NSS002");
    }

    [Fact]
    public void NSS003ReportedWhenStatePropertyHasWrongReturnType()
    {
        var source = """
        using Nalu.SharpState;

        namespace Sample;

        public class Ctx { }

        [StateMachineDefinition(typeof(Ctx))]
        public static partial class WrongState
        {
            [StateTriggerDefinition] static partial void Go();

            [StateDefinition] private static string A => "nope";
        }
        """;
        GetDiagnostics(source).Should().Contain(d => d.Id == "NSS003");
    }

    [Fact]
    public void NSS003NotReportedForGenericRuntimeInterfaceReturnType()
    {
        var source = """
        using Nalu.SharpState;

        namespace Sample;

        public class Ctx { }

        [StateMachineDefinition(typeof(Ctx))]
        public static partial class GenericConfigReturnType
        {
            [StateTriggerDefinition] static partial void Go();

            [StateDefinition(Initial = true)]
            private static global::Nalu.SharpState.IStateConfiguration<Ctx, State, Trigger> A { get; } = ConfigureState();
        }
        """;

        GetDiagnostics(source).Should().NotContain(d => d.Id == "NSS003");
    }

    [Fact]
    public void NSS004ReportedWhenTriggerIsNotPartialVoid()
    {
        var source = """
        using Nalu.SharpState;

        namespace Sample;

        public class Ctx { }

        [StateMachineDefinition(typeof(Ctx))]
        public static partial class BadTrigger
        {
            [StateTriggerDefinition] static void Go() { }
            [StateDefinition(Initial = true)] private static IStateConfiguration A { get; } = ConfigureState();
        }
        """;
        GetDiagnostics(source).Should().Contain(d => d.Id == "NSS004");
    }

    [Fact]
    public void NSS005ReportedWhenSubstatemachineIsNotPartial()
    {
        var source = """
        using Nalu.SharpState;

        namespace Sample;

        public class Ctx { }

        [StateMachineDefinition(typeof(Ctx))]
        public static partial class M
        {
            [StateTriggerDefinition] static partial void Go();

            [StateDefinition(Initial = true)] private static IStateConfiguration A { get; } = ConfigureState();
            [StateDefinition] private static IStateConfiguration B { get; } = ConfigureState();

            [SubStateMachine(parent: State.A)]
            private class NotPartialRegion { }
        }
        """;
        GetDiagnostics(source).Should().Contain(d => d.Id == "NSS005");
    }

    [Fact]
    public void NSS006ReportedWhenContainingTypeIsNotPartial()
    {
        var source = """
        using Nalu.SharpState;

        namespace Sample;

        public class Ctx { }

        public class Outer
        {
            [StateMachineDefinition(typeof(Ctx))]
            public static partial class M
            {
                [StateTriggerDefinition] static partial void Go();
                [StateDefinition(Initial = true)] private static IStateConfiguration A { get; } = ConfigureState();
            }
        }
        """;
        GetDiagnostics(source).Should().Contain(d => d.Id == "NSS006");
    }

    [Fact]
    public void NSS007ReportedWhenParentIsNotInEnclosingRegion()
    {
        var source = """
        using Nalu.SharpState;

        namespace Sample;

        public class Ctx { }

        [StateMachineDefinition(typeof(Ctx))]
        public static partial class M
        {
            [StateTriggerDefinition] static partial void Go();

            [StateDefinition(Initial = true)] private static IStateConfiguration A { get; } = ConfigureState();

            [SubStateMachine(parent: State.Nowhere)]
            private partial class Region
            {
                [StateDefinition(Initial = true)] private static IStateConfiguration B { get; } = ConfigureState();
            }
        }
        """;
        GetDiagnostics(source).Should().Contain(d => d.Id == "NSS007");
    }

    [Fact]
    public void NSS008ReportedWhenRegionHasNoInitialState()
    {
        var source = """
        using Nalu.SharpState;

        namespace Sample;

        public class Ctx { }

        [StateMachineDefinition(typeof(Ctx))]
        public static partial class M
        {
            [StateTriggerDefinition] static partial void Go();

            [StateDefinition(Initial = true)] private static IStateConfiguration A { get; } = ConfigureState();

            [SubStateMachine(parent: State.A)]
            private partial class Region
            {
                [StateDefinition] private static IStateConfiguration B { get; } = ConfigureState();
            }
        }
        """;
        GetDiagnostics(source).Should().Contain(d => d.Id == "NSS008");
    }

    [Fact]
    public void NSS009ReportedWhenTriggerIsDeclaredInsideSubstatemachine()
    {
        var source = """
        using Nalu.SharpState;

        namespace Sample;

        public class Ctx { }

        [StateMachineDefinition(typeof(Ctx))]
        public static partial class M
        {
            [StateTriggerDefinition] static partial void Go();

            [StateDefinition(Initial = true)] private static IStateConfiguration A { get; } = ConfigureState();

            [SubStateMachine(parent: State.A)]
            private partial class Region
            {
                [StateDefinition(Initial = true)] private static IStateConfiguration B { get; } = ConfigureState();
                [StateTriggerDefinition] static partial void Nope();
            }
        }
        """;
        GetDiagnostics(source).Should().Contain(d => d.Id == "NSS009");
    }

    [Fact]
    public void GeneratedSurfaceSupportsDynamicTargetsAndTwoPhaseBuilder()
    {
        var source = """
        using Nalu.SharpState;

        namespace Sample;

        public class Ctx
        {
            public bool UseB { get; set; }
            public int Counter { get; set; }
        }

        [StateMachineDefinition(typeof(Ctx))]
        public static partial class DynamicMachine
        {
            [StateTriggerDefinition] static partial void Go(int step);

            [StateDefinition(Initial = true)]
            private static IStateConfiguration A { get; } = ConfigureState()
                .OnGo(t => t
                    .When((ctx, args) => args.Step >= 0)
                    .TransitionTo((ctx, args) => ctx.UseB && args.Step == ctx.Counter ? State.B : State.C)
                    .Invoke((ctx, args) => ctx.Counter += args.Step)
                    .ReactAsync((_, _, _) => default));

            [StateDefinition] private static IStateConfiguration B { get; } = ConfigureState();
            [StateDefinition] private static IStateConfiguration C { get; } = ConfigureState();
        }
        """;

        GetDiagnostics(source).Should().BeEmpty();
        GetCompilationErrors(source).Should().BeEmpty();
    }

    [Fact]
    public void NSS010ReportedWhenRegionHasMultipleInitialStates()
    {
        var source = """
        using Nalu.SharpState;

        namespace Sample;

        public class Ctx { }

        [StateMachineDefinition(typeof(Ctx))]
        public static partial class M
        {
            [StateTriggerDefinition] static partial void Go();

            [StateDefinition(Initial = true)] private static IStateConfiguration A { get; } = ConfigureState();
            [StateDefinition(Initial = true)] private static IStateConfiguration B { get; } = ConfigureState();
        }
        """;

        GetDiagnostics(source).Should().Contain(d => d.Id == "NSS010");
    }

    [Fact]
    public void TriggerWithSeventeenParametersDoesNotReportGeneratorDiagnostic()
    {
        var source = """
        using Nalu.SharpState;

        namespace Sample;

        public class Ctx { }

        [StateMachineDefinition(typeof(Ctx))]
        public static partial class M
        {
            [StateTriggerDefinition] static partial void TooMany(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j, int k, int l, int m, int n, int o, int p, int q);

            [StateDefinition(Initial = true)]
            private static IStateConfiguration A { get; } = ConfigureState()
                .OnTooMany(t => t.TransitionTo(State.A).Invoke((_, _) => { }));
        }
        """;
        GetDiagnostics(source).Should().BeEmpty();
        GetCompilationErrors(source).Should().BeEmpty();
    }
}
