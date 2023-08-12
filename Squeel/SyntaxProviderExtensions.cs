using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Squeel;

public sealed record SqueelCallSite
{
    public required InvocationExpressionSyntax Invocation { get; init; }

    public required SemanticModel SemanticModel { get; init; }
}

public static class SyntaxProviderExtensions
{
    public static IncrementalValueProvider<string?> ForSqueelConnectionString(this IncrementalValueProvider<AnalyzerConfigOptionsProvider> provider)
    {
        return provider.Select(static (config, ct) =>
        {
            return config.GlobalOptions.TryGetValue("build_property.SqueelConnectionString", out var value)
                ? value.Replace('+', ';')
                : null;
        });
    }
}
