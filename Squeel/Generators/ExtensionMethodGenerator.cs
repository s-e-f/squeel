using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Squeel.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class ExtensionMethodGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource($"{GeneratedFileOptions.MethodName}Signature.g.cs", SourceText.From($$"""
                {{GeneratedFileOptions.Header}}

                namespace {{GeneratedFileOptions.Namespace}};

                {{GeneratedFileOptions.Attribute}}
                internal static class SqueelDbConnectionExtensions
                {
                    {{GeneratedFileOptions.Attribute}}
                    public static global::System.Collections.Generic.IAsyncEnumerable<T>
                        {{GeneratedFileOptions.MethodName}}<T>
                    (
                        this global::Npgsql.NpgsqlConnection connection,
                        ref global::{{GeneratedFileOptions.Namespace}}.SqueelInterpolatedStringHandler query,
                        global::System.Threading.CancellationToken ct = default
                    )
                    {
                        throw new global::System.InvalidOperationException("This call failed to be intercepted by the Squeel source generator");
                    }

                    {{GeneratedFileOptions.Attribute}}
                    public static global::System.Threading.Tasks.Task<int>
                        ExecuteAsync
                    (
                        this global::Npgsql.NpgsqlConnection connection,
                        ref global::{{GeneratedFileOptions.Namespace}}.SqueelInterpolatedStringHandler query,
                        global::System.Threading.CancellationToken ct = default
                    )
                    {
                        throw new global::System.InvalidOperationException("This call failed to be intercepted by the Squeel source generator");
                    }
                }
                """, Encoding.UTF8));
        });
    }
}
