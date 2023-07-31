using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

    private static bool Filter(SyntaxNode node, CancellationToken ct) => node is InvocationExpressionSyntax;

    private static SqueelCallSite Transform(GeneratorSyntaxContext context, CancellationToken ct)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        
        return new() 
        {
            Invocation = invocation,
            SemanticModel = context.SemanticModel,
        };
    }

    public static IncrementalValuesProvider<SqueelCallSite> ForCallsToSqueelQueryMethod(this SyntaxValueProvider provider)
    {
        return provider.CreateSyntaxProvider(Filter, Transform)
            .Where(info =>
            {
                var invocationSymbol = info.SemanticModel.GetSymbolInfo(info.Invocation);

                var format = new SymbolDisplayFormat(
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                    genericsOptions: SymbolDisplayGenericsOptions.None,
                    memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
                    extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
                    parameterOptions: SymbolDisplayParameterOptions.IncludeType
                    );

                var check = invocationSymbol.Symbol?.ToDisplayString(format);

                return check is "Squeel.SqueelDbConnectionExtensions.QueryAsync";
            });
    }
}
