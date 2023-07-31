using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Squeel;

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
                internal static partial class SqueelDbConnectionExtensions
                {
                    {{GeneratedFileOptions.Attribute}}
                    public static partial global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<T>> {{GeneratedFileOptions.MethodName}}<T>(
                        this global::Npgsql.NpgsqlConnection connection,
                        ref global::{{GeneratedFileOptions.Namespace}}.SqueelInterpolatedStringHandler query,
                        global::System.Threading.CancellationToken ct);
                }
                """, Encoding.UTF8));
        });
    }
}
