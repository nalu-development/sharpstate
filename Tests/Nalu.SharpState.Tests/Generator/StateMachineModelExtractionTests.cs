using FluentAssertions;
using Nalu.SharpState.Generators.Diagnostics;

namespace Nalu.SharpState.Tests.Generator;

public class StateMachineModelExtractionTests
{
    [Fact]
    public void StateModel_IsInitial_is_true_when_Initial_is_set()
    {
        var model = GeneratorDriverHelper.GetStateMachineModel(
            """
            using Nalu.SharpState;

            namespace N;

            public class C { }

            [StateMachineDefinition(typeof(C))]
            public static partial class M
            {
                [StateTriggerDefinition] static partial void Go();

                [StateDefinition(Initial = true)]
                private static IStateConfiguration A { get; } = null!;

                [StateDefinition]
                private static IStateConfiguration B { get; } = null!;
            }
            """);

        var initial = model.States.Should().ContainSingle(s => s.IsInitial).Subject;
        initial.Name.Should().Be("A");
    }

    [Fact]
    public void Multiple_initial_states_in_one_region_produces_MultipleInitialStates_diagnostic()
    {
        var model = GeneratorDriverHelper.GetStateMachineModel(
            """
            using Nalu.SharpState;

            namespace N;

            public class C { }

            [StateMachineDefinition(typeof(C))]
            public static partial class M
            {
                [StateTriggerDefinition] static partial void Go();

                [StateDefinition(Initial = true)]
                private static IStateConfiguration S1 { get; } = null!;

                [StateDefinition(Initial = true)]
                private static IStateConfiguration S2 { get; } = null!;

                [StateDefinition]
                private static IStateConfiguration S3 { get; } = null!;
            }
            """);

        model.Diagnostics.Should()
            .Contain(d => d.Descriptor == Descriptors.MultipleInitialStates
                && d.MessageArgs.Count > 0
                && d.MessageArgs[0] == "M");
    }

    [Fact]
    public void Containing_type_struct_uses_struct_keyword_in_ContainingTypeModel()
    {
        var model = GeneratorDriverHelper.GetStateMachineModel(
            """
            using Nalu.SharpState;

            namespace N;

            public class C { }

            public struct OuterStruct
            {
                [StateMachineDefinition(typeof(C))]
                public static partial class M
                {
                    [StateTriggerDefinition] static partial void T();

                    [StateDefinition(Initial = true)]
                    public static IStateConfiguration A { get; } = null!;
                }
            }
            """);

        model.ContainingTypes.Should().ContainSingle(c => c.Name == "OuterStruct");
        model.ContainingTypes[0].Keyword.Should().Be("struct");
    }

    [Fact]
    public void Containing_type_interface_uses_interface_keyword_in_ContainingTypeModel()
    {
        var model = GeneratorDriverHelper.GetStateMachineModel(
            """
            using Nalu.SharpState;

            namespace N;

            public class C { }

            public interface IContainer
            {
                [StateMachineDefinition(typeof(C))]
                public static partial class M
                {
                    [StateTriggerDefinition] static partial void T();

                    [StateDefinition(Initial = true)]
                    public static IStateConfiguration A { get; } = null!;
                }
            }
            """);

        model.ContainingTypes.Should().ContainSingle(c => c.Name == "IContainer");
        model.ContainingTypes[0].Keyword.Should().Be("interface");
    }

    [Fact]
    public void Containing_record_struct_uses_record_struct_keyword()
    {
        var model = GeneratorDriverHelper.GetStateMachineModel(
            """
            using Nalu.SharpState;

            namespace N;

            public class C { }

            public record struct Rs
            {
                [StateMachineDefinition(typeof(C))]
                public static partial class M
                {
                    [StateTriggerDefinition] static partial void T();

                    [StateDefinition(Initial = true)]
                    public static IStateConfiguration A { get; } = null!;
                }
            }
            """);

        model.ContainingTypes.Should().ContainSingle(c => c.Name == "Rs");
        model.ContainingTypes[0].Keyword.Should().Be("record struct");
    }

    [Fact]
    public void Containing_type_private_protected_uses_AccessibilityString_branch()
    {
        var model = GeneratorDriverHelper.GetStateMachineModel(
            """
            using Nalu.SharpState;

            namespace N;

            public class C { }

            public class O
            {
                private protected partial class Inner
                {
                    [StateMachineDefinition(typeof(C))]
                    public static partial class M
                    {
                        [StateTriggerDefinition] static partial void T();

                        [StateDefinition(Initial = true)]
                        public static IStateConfiguration A { get; } = null!;
                    }
                }
            }
            """);

        model.ContainingTypes.Count.Should().Be(2);
        model.ContainingTypes[0].Name.Should().Be("O");
        model.ContainingTypes[0].Accessibility.Should().Be("public");
        model.ContainingTypes[1].Name.Should().Be("Inner");
        model.ContainingTypes[1].Accessibility.Should().Be("private protected");
    }

    [Fact]
    public void Containing_type_protected_internal_uses_AccessibilityString_branch()
    {
        var model = GeneratorDriverHelper.GetStateMachineModel(
            """
            using Nalu.SharpState;

            namespace N;

            public class C { }

            public class O
            {
                protected internal partial class Inner
                {
                    [StateMachineDefinition(typeof(C))]
                    public static partial class M
                    {
                        [StateTriggerDefinition] static partial void T();

                        [StateDefinition(Initial = true)]
                        public static IStateConfiguration A { get; } = null!;
                    }
                }
            }
            """);

        model.ContainingTypes.Count.Should().Be(2);
        model.ContainingTypes[0].Name.Should().Be("O");
        model.ContainingTypes[0].Accessibility.Should().Be("public");
        model.ContainingTypes[1].Name.Should().Be("Inner");
        model.ContainingTypes[1].Accessibility.Should().Be("protected internal");
    }

    [Fact]
    public void Containing_record_reference_type_uses_record_keyword()
    {
        var model = GeneratorDriverHelper.GetStateMachineModel(
            """
            using Nalu.SharpState;

            namespace N;

            public class C { }

            public record OuterRecord
            {
                [StateMachineDefinition(typeof(C))]
                public static partial class M
                {
                    [StateTriggerDefinition] static partial void T();

                    [StateDefinition(Initial = true)]
                    public static IStateConfiguration A { get; } = null!;
                }
            }
            """);

        model.ContainingTypes.Should().ContainSingle(t => t.Name == "OuterRecord");
        model.ContainingTypes[0].Keyword.Should().Be("record");
    }

    [Fact]
    public void State_machine_type_parameters_appear_in_TypeParameterList()
    {
        var model = GeneratorDriverHelper.GetStateMachineModel(
            """
            using Nalu.SharpState;

            namespace N;

            public class C { }

            [StateMachineDefinition(typeof(C))]
            public static partial class M<TContext, TExtra>
            {
                [StateTriggerDefinition] static partial void T();

                [StateDefinition(Initial = true)]
                public static IStateConfiguration A { get; } = null!;
            }
            """);

        model.TypeParameters.Should().Be("<TContext, TExtra>");
    }

    [Fact]
    public void Same_state_property_name_in_two_sub_regions_fires_DuplicateName()
    {
        var model = GeneratorDriverHelper.GetStateMachineModel(
            """
            using Nalu.SharpState;

            namespace N;

            public class C { }

            [StateMachineDefinition(typeof(C))]
            public static partial class M
            {
                [StateTriggerDefinition] static partial void T();

                [StateDefinition(Initial = true)]
                public static IStateConfiguration Root { get; } = null!;

                [StateDefinition] public static IStateConfiguration S1 { get; } = null!;

                [StateDefinition] public static IStateConfiguration S2 { get; } = null!;

                [SubStateMachine(S1)]
                public partial class Sub1
                {
                    [StateDefinition(Initial = true)]
                    public static IStateConfiguration Dup { get; } = null!;
                }

                [SubStateMachine(S2)]
                public partial class Sub2
                {
                    [StateDefinition(Initial = true)]
                    public static IStateConfiguration Dup { get; } = null!;
                }
            }
            """);

        model.Diagnostics.Should()
            .Contain(d => d.Descriptor == Descriptors.DuplicateName
                && d.MessageArgs[0] == "state" && d.MessageArgs[1] == "Dup");
    }

    [Fact]
    public void Record_state_machine_uses_classKeyword_record()
    {
        var model = GeneratorDriverHelper.GetStateMachineModel(
            """
            using Nalu.SharpState;

            namespace N;

            public class C { }

            [StateMachineDefinition(typeof(C))]
            public partial record R
            {
                [StateTriggerDefinition] static partial void T();

                [StateDefinition(Initial = true)]
                public static IStateConfiguration A { get; } = null!;
            }
            """);

        model.ClassKeyword.Should().Be("record");
    }

    [Fact]
    public void SubStateMachine_resolves_default_enum_to_first_member_name()
    {
        var model = GeneratorDriverHelper.GetStateMachineModel(
            """
            using Nalu.SharpState;

            namespace N;

            public class C { }

            public enum E { ParentX, Y }

            [StateMachineDefinition(typeof(C))]
            public static partial class M
            {
                [StateTriggerDefinition] static partial void T();

                [StateDefinition(Initial = true)]
                public static IStateConfiguration Root { get; } = null!;

                [StateDefinition] public static IStateConfiguration ParentX { get; } = null!;

                [SubStateMachine(default(E))]
                public partial class Sub
                {
                    [StateDefinition(Initial = true)]
                    public static IStateConfiguration ChildA { get; } = null!;
                }
            }
            """);

        model.States.First(s => s.Name == "ChildA").ParentState.Should().Be("ParentX");
    }
}
