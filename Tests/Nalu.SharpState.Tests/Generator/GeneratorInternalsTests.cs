using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nalu.SharpState.Generators.Emit;
using Nalu.SharpState.Generators.Model;

namespace Nalu.SharpState.Tests.Generator;

public class GeneratorInternalsTests
{
    [Fact]
    public void SourceWriterBlockWithHeaderWritesLineBeforeOpenBrace()
    {
        var w = new SourceWriter();
        using (w.Block("class X"))
        {
            w.WriteLine("int y;");
        }

        w.ToString().Should().Contain("class X");
        w.ToString().Should().Contain("int y;");
    }

    [Fact]
    public void SourceWriterIndentGetSet()
    {
        var w = new SourceWriter();
        w.Indent = 1;
        w.Indent.Should().Be(1);
    }

    [Fact]
    public void LocationInfoFromLocationNullReturnsNull()
    {
        LocationInfo.FromLocation(null).Should().BeNull();
    }

    [Fact]
    public void EquatableArrayTCoversEqualsGetHashCodeAndOperators()
    {
        var a1 = new EquatableArray<int>(new[] { 1, 2, 3 });
        var a2 = new EquatableArray<int>(new[] { 1, 2, 3 });
        var a3 = new EquatableArray<int>(new[] { 1, 2, 4 });
        var empty1 = new EquatableArray<int>(Array.Empty<int>());
#pragma warning disable CS8604 // Intentional null to cover null-source branch in EquatableArray ctor.
        var empty2 = new EquatableArray<int>(null as IEnumerable<int>);
#pragma warning restore CS8604

        (a1 == a2).Should().BeTrue();
        (a1 != a3).Should().BeTrue();
        a1.Equals((object)a2).Should().BeTrue();
        a1.Equals(1).Should().BeFalse();
        a1.GetHashCode().Should().NotBe(0);

        empty1.Equals(empty2).Should().BeTrue();
        a1.GetHashCode();
        empty1.GetHashCode();

        var def = default(EquatableArray<int>);
        var emptyImm = new EquatableArray<int>(ImmutableArray<int>.Empty);
        def.Equals(emptyImm).Should().BeFalse();
        def.Equals(def).Should().BeTrue();
        new EquatableArray<int>(new[] { 1 }).Equals(new EquatableArray<int>(new[] { 1, 2 })).Should().BeFalse();
        def.GetHashCode().Should().Be(0);

        _ = ((IEnumerable)empty1).GetEnumerator();

        var noItems = 0;
        foreach (var _ in def)
        {
            noItems++;
        }

        noItems.Should().Be(0);

        _ = EquatableArray<int>.Empty;
        _ = new EquatableArray<string>(new[] { null!, "a" }).GetHashCode();

        var extId = (Func<ExpressionSyntax, string?>)Delegate.CreateDelegate(
            typeof(Func<ExpressionSyntax, string?>),
            typeof(StateMachineModel).GetMethod(
                "ExtractIdentifierTail",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(ExpressionSyntax) },
                modifiers: null)!);
        extId(SyntaxFactory.ParseExpression("a.b")).Should().Be("b");
        extId(SyntaxFactory.ParseExpression("x")).Should().Be("x");
        extId(SyntaxFactory.ParseExpression("1+1")).Should().BeNull();

        var acc = (Func<Accessibility, string>)Delegate.CreateDelegate(
            typeof(Func<Accessibility, string>),
            typeof(StateMachineModel).GetMethod(
                "AccessibilityString",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(Accessibility) },
                modifiers: null)!);
        acc(Accessibility.Protected).Should().Be("protected");
        acc(Accessibility.Private).Should().Be("private");
        acc(Accessibility.ProtectedOrInternal).Should().Be("protected internal");
        acc((Accessibility)999).Should().Be("internal");

        var exEnum = (Func<TypedConstant, string?>)Delegate.CreateDelegate(
            typeof(Func<TypedConstant, string?>),
            typeof(StateMachineModel).GetMethod(
                "ExtractEnumName",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(TypedConstant) },
                modifiers: null)!);
        exEnum(default).Should().BeNull();
    }

    [Fact]
    public void ExtractEnumNameReturnsNullWhenEnumValueMatchesNoField()
    {
        var exEnum = (Func<TypedConstant, string?>)Delegate.CreateDelegate(
            typeof(Func<TypedConstant, string?>),
            typeof(StateMachineModel).GetMethod(
                "ExtractEnumName",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(TypedConstant) },
                modifiers: null)!);

        const string source = """
            using Nalu.SharpState;

            public enum E { A, B }

            public partial class P
            {
                [SubStateMachine((E)99)]
                public partial class Sub { }
            }
            """;

        var compilation = GeneratorDriverHelper.CreateCompilation(source);
        compilation
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Should()
            .BeEmpty();

        var tree = compilation.SyntaxTrees[0];
        var sub = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(s => s.Identifier.Text == "Sub");
        var model = compilation.GetSemanticModel(tree);
        var symbol = model.GetDeclaredSymbol(sub)!;
        var attr = symbol.GetAttributes().Single(a => a.AttributeClass?.Name == "SubStateMachineAttribute");
        exEnum(attr.ConstructorArguments[0]).Should().BeNull();
    }

    [Fact]
    public void ExtractEnumNameReturnsFieldNameForEnumPositional()
    {
        var exEnum = (Func<TypedConstant, string?>)Delegate.CreateDelegate(
            typeof(Func<TypedConstant, string?>),
            typeof(StateMachineModel).GetMethod(
                "ExtractEnumName",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(TypedConstant) },
                modifiers: null)!);

        const string source = """
            using Nalu.SharpState;

            public enum E { ParentX, Y }

            public partial class P
            {
                [SubStateMachine(E.Y)]
                public partial class Sub { }
            }
            """;

        var compilation = GeneratorDriverHelper.CreateCompilation(source);
        compilation
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Should()
            .BeEmpty();

        var tree = compilation.SyntaxTrees[0];
        var sub = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(s => s.Identifier.Text == "Sub");
        var model = compilation.GetSemanticModel(tree);
        var symbol = model.GetDeclaredSymbol(sub)!;
        var attr = symbol.GetAttributes().Single(a => a.AttributeClass?.Name == "SubStateMachineAttribute");
        exEnum(attr.ConstructorArguments[0]).Should().Be("Y");
    }
}
