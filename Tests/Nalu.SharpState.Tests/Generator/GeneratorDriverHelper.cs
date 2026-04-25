using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nalu.SharpState.Generators;
using Nalu.SharpState.Generators.Model;

namespace Nalu.SharpState.Tests.Generator;

internal static class GeneratorDriverHelper
{
    private static readonly ImmutableArray<MetadataReference> _references = BuildReferences();

    /// <summary>
    /// Parses <paramref name="source"/>, compiles it, and builds a <see cref="StateMachineModel"/> for the first
    /// <c>[StateMachineDefinition]</c> class declaration (metadata-only; the generator pipeline is not run).
    /// </summary>
    /// <summary>
    /// Compiles a single source file with the same references as <see cref="RunGenerator"/> (no generator run).
    /// </summary>
    internal static CSharpCompilation CreateCompilation(string source, string assemblyName = "TestCompile")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        return CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: [syntaxTree],
            references: _references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    public static StateMachineModel GetStateMachineModel(string source, CancellationToken ct = default)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        var compilation = CSharpCompilation.Create(
            assemblyName: "ModelExtractionAssembly",
            syntaxTrees: [syntaxTree],
            references: _references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        foreach (var typeDecl in syntaxTree.GetRoot(ct).DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (typeDecl is not ClassDeclarationSyntax and not RecordDeclarationSyntax)
            {
                continue;
            }

            if (semanticModel.GetDeclaredSymbol(typeDecl, ct) is not INamedTypeSymbol named)
            {
                continue;
            }

            var hasDefinition = named.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString() == "Nalu.SharpState.StateMachineDefinitionAttribute");
            if (!hasDefinition)
            {
                continue;
            }

            return StateMachineModel.FromSymbol(named, typeDecl, ct)
                ?? throw new InvalidOperationException("StateMachineModel.FromSymbol returned null.");
        }

        throw new InvalidOperationException("No [StateMachineDefinition] class or record found in source.");
    }

    public static GeneratorDriverRunResult RunGenerator(string source, out Compilation outputCompilation)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTestAssembly",
            syntaxTrees: [syntaxTree],
            references: _references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var generator = new StateMachineGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            parseOptions: (CSharpParseOptions) syntaxTree.Options,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out _);
        return driver.GetRunResult();
    }

    private static ImmutableArray<MetadataReference> BuildReferences()
    {
        var trustedAssembliesPaths = ((string?) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        var refs = ImmutableArray.CreateBuilder<MetadataReference>();
        foreach (var path in trustedAssembliesPaths)
        {
            refs.Add(MetadataReference.CreateFromFile(path));
        }

        refs.Add(MetadataReference.CreateFromFile(typeof(StateMachineDefinitionAttribute).Assembly.Location));
        return refs.ToImmutable();
    }

    public static VerifySettings DefaultSettings([CallerFilePath] string? sourceFilePath = null)
    {
        var settings = new VerifySettings();
        settings.UseDirectory("Snapshots");
        return settings;
    }
}
