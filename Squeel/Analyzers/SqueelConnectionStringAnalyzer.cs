using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Squeel.Diagnostics;
using System.Collections.Immutable;

namespace Squeel.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SqueelConnectionStringAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptors.MissingSqueelConnectionString);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationAction(compilationContext =>
        {
            if (!compilationContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.SqueelConnectionString", out var connectionString) || string.IsNullOrWhiteSpace(connectionString))
            {
                var diagnostic = Errors.MissingSqueelConnectionString();
                compilationContext.ReportDiagnostic(diagnostic);
            }
        });
    }
}
