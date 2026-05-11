using FluentAssertions;
using Microsoft.CodeAnalysis;

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
    public void NSS001_reported_when_class_is_not_partial()
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
    public void NSS002_reported_on_duplicate_trigger()
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
    public void NSS002_reported_when_duplicate_trigger_name_overloads_conflict_with_enum_generation()
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
    public void NSS003_reported_when_state_property_has_wrong_return_type()
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
    public void NSS003_not_reported_for_generic_runtime_interface_return_type()
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
    public void NSS004_reported_when_trigger_is_not_partial_void()
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
    public void NSS005_reported_when_substatemachine_is_not_partial()
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
    public void NSS006_reported_when_containing_type_is_not_partial()
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
    public void NSS007_reported_when_parent_is_not_in_enclosing_region()
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
    public void NSS008_reported_when_region_has_no_initial_state()
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
    public void NSS009_reported_when_trigger_is_declared_inside_substatemachine()
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
    public void Generated_surface_supports_dynamic_targets_and_two_phase_builder()
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
                    .Target((ctx, args) => ctx.UseB && args.Step == ctx.Counter ? State.B : State.C)
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
    public void NSS010_reported_when_region_has_multiple_initial_states()
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
    public void Trigger_with_seventeen_parameters_does_not_report_generator_diagnostic()
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
                .OnTooMany(t => t.Target(State.A).Invoke((_, _) => { }));
        }
        """;
        GetDiagnostics(source).Should().BeEmpty();
        GetCompilationErrors(source).Should().BeEmpty();
    }
}
