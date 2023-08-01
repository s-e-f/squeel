using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Npgsql;
using Squeel.Analyzers;
using Squeel.Generators;
using Xunit.Abstractions;

namespace Squeel.GeneratorTests;

public static class SqueelTestContext
{
    public static SqueelTestResult Run(string connectionString, ITestOutputHelper output, SyntaxTree code, IEnumerable<MetadataReference>? additionalReferences = null, CancellationToken ct = default)
    {
        var squeelConnectionStringProvider = new SqueelTestAnalyzerConfigOptionsProvider(connectionString);

        var compilation = NetCoreCompilation.Create(
            ImmutableArray.Create(code),
            (additionalReferences ?? Enumerable.Empty<MetadataReference>()).Append(MetadataReference.CreateFromFile(typeof(NpgsqlDataSource).Assembly.Location)))
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new SqueelConnectionStringAnalyzer()), new AnalyzerOptions(Enumerable.Empty<AdditionalText>().ToImmutableArray(), squeelConnectionStringProvider), cancellationToken: ct);

        var analysisResults = compilation.GetAnalysisResultAsync(ct).GetAwaiter().GetResult();

        var generators = new IIncrementalGenerator[]
        {
            new EntityGenerator(),
            new ExtensionMethodGenerator(),
            new SqueelInterpolatedStringHandlerGenerator(),
            new QueryGenerator(),
        };

        var driver = CSharpGeneratorDriver.Create(generators)
            .WithUpdatedAnalyzerConfigOptions(squeelConnectionStringProvider)
            .RunGeneratorsAndUpdateCompilation(compilation.Compilation, out var newCompilation, out _, ct);

        var runResult = driver.GetRunResult();

        var result = new SqueelTestResult
        {
            Errors = newCompilation.GetDiagnostics(ct).Where(d => d.WarningLevel is 0 && !d.IsWarningAsError).ToImmutableArray(),
            GeneratorDiagnostics = runResult.Diagnostics,
            GeneratedFiles = runResult.GeneratedTrees,
            AnalyzerDiagnostics = analysisResults,
        };

        if (result.GeneratorDiagnostics.Any())
        {
            output.WriteLine($"# Diagnostics");
            output.WriteLine("");
            foreach (var diag in result.GeneratorDiagnostics)
                output.WriteLine($"{diag.Location.SourceTree?.FilePath}:{diag.Location.GetLineSpan().StartLinePosition.Line} {diag.GetMessage()}");
            output.WriteLine("");
        }

        if (result.Errors.Any())
        {
            output.WriteLine($"# Errors");
            output.WriteLine("");
            foreach (var error in result.Errors)
                output.WriteLine($"{error.Location.GetLineSpan()} {error.GetMessage()}");
            output.WriteLine("");
        }

        foreach (var file in result.GeneratedFiles.Where(f => f.FilePath.StartsWith("Squeel\\Squeel.EntityGenerator") || f.FilePath.StartsWith("Squeel\\Squeel.QueryGenerator")))
        {
            output.WriteLine($"""
                # {file.FilePath}

                ```csharp
                """);
            var text = file.GetText(ct);
            var lineCount = text.Lines.Count;
            var gutterSize = lineCount.ToString().Length;
            foreach (var line in text.Lines)
            {
                var gutter = line.LineNumber.ToString().PadLeft(gutterSize);
                output.WriteLine($"    {gutter}  {line}");
            }

            output.WriteLine("""
                ```

                ---

                """);
        }

        return result;
    }
}

public readonly record struct SqueelTestResult
{
    public required ImmutableArray<Diagnostic> Errors { get; init; }

    public required ImmutableArray<SyntaxTree> GeneratedFiles { get; init; }

    public required ImmutableArray<Diagnostic> GeneratorDiagnostics { get; init; }

    public required AnalysisResult AnalyzerDiagnostics { get; init; }
}
