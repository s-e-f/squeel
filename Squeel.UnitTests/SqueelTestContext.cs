using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Squeel.UnitTests;

internal sealed class SqueelTestAnalyzerConfigOptions : AnalyzerConfigOptions
{
    private readonly string _connectionString;

    public SqueelTestAnalyzerConfigOptions(string connectionString)
    {
        _connectionString = connectionString;
    }

    public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        if (key is not "build_property.SqueelConnectionString")
        {
            value = null;
            return false;
        }

        value = _connectionString;
        return true;
    }
}

internal sealed class SqueelTestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    public SqueelTestAnalyzerConfigOptionsProvider(string connectionString)
    {
        GlobalOptions = new SqueelTestAnalyzerConfigOptions(connectionString);
    }

    public override AnalyzerConfigOptions GlobalOptions { get; }

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
    {
        return GlobalOptions;
    }

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
    {
        return GlobalOptions;
    }
}

public static class SqueelTestContext<TGenerator> where TGenerator : IIncrementalGenerator, new()
{
    public static SqueelTestResult Run(string connectionString, SyntaxTree code, IEnumerable<MetadataReference>? additionalReferences = null, CancellationToken ct = default)
    {
        var compilation = NetCoreCompilation.Create(ImmutableArray.Create(code), additionalReferences);

        compilation = GenerateBaseTypes(connectionString, compilation, out var baseDiagnostics, ct);

        var driver = CSharpGeneratorDriver.Create(new TGenerator())
            .WithUpdatedAnalyzerConfigOptions(new SqueelTestAnalyzerConfigOptionsProvider(connectionString))
            .RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out _, ct);

        var result = driver.GetRunResult();

        return new SqueelTestResult
        {
            Errors = newCompilation.GetDiagnostics(ct).Where(d => d.WarningLevel is 0 && !d.IsWarningAsError).ToImmutableArray(),
            GeneratorDiagnostics = baseDiagnostics.Concat(result.Diagnostics).ToImmutableArray(),
            GeneratedFiles = result.GeneratedTrees,
        };
    }

    private static CSharpCompilation GenerateBaseTypes(string connectionString, CSharpCompilation compilation, out ImmutableArray<Diagnostic> baseDiagnostics, CancellationToken ct = default)
    {
        var driver = CSharpGeneratorDriver.Create(new QueryGenerator())
            .WithUpdatedAnalyzerConfigOptions(new SqueelTestAnalyzerConfigOptionsProvider(connectionString))
            .RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out _, ct);

        var result = driver.GetRunResult();
        baseDiagnostics = result.Diagnostics;

        return (CSharpCompilation)newCompilation;
    }
}

public readonly record struct SqueelTestResult
{
    public required ImmutableArray<Diagnostic> Errors { get; init; }

    public required ImmutableArray<SyntaxTree> GeneratedFiles { get; init; }

    public required ImmutableArray<Diagnostic> GeneratorDiagnostics { get; init; }
}
