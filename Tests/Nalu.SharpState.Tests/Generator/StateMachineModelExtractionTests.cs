using FluentAssertions;
using Nalu.SharpState.Generators.Diagnostics;

namespace Nalu.SharpState.Tests.Generator;

public class StateMachineModelExtractionTests
{
    [Fact]
    public void StateModelIsInitialIsTrueWhenInitialIsSet()
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
    public void MultipleInitialStatesInOneRegionProducesMultipleInitialStatesDiagnostic()
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
    public void ContainingTypeStructUsesStructKeywordInContainingTypeModel()
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
    public void ContainingTypeInterfaceUsesInterfaceKeywordInContainingTypeModel()
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
    public void ContainingRecordStructUsesRecordStructKeyword()
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
    public void ContainingTypePrivateProtectedUsesAccessibilityStringBranch()
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
    public void ContainingTypeProtectedInternalUsesAccessibilityStringBranch()
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
    public void ContainingRecordReferenceTypeUsesRecordKeyword()
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
    public void StateMachineTypeParametersAppearInTypeParameterList()
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
    public void SameStatePropertyNameInTwoSubRegionsFiresDuplicateName()
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
    public void RecordStateMachineUsesClassKeywordRecord()
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
    public void SubStateMachineResolvesDefaultEnumToFirstMemberName()
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
