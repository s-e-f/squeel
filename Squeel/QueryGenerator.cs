using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Squeel;

[Generator(LanguageNames.CSharp)]
public sealed class QueryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForCallsToSqueelQueryMethod()
            .Where(callSite =>
            {
                if (callSite.Invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                    return false;

                if (memberAccess.Name is not GenericNameSyntax name)
                    return false;

                if (name.TypeArgumentList.Arguments.Count is not 1)
                    return false;

                return true;
            })
            .Select((callSite, ct) =>
            {
                var member = (MemberAccessExpressionSyntax)callSite.Invocation.Expression;
                var name = (GenericNameSyntax)member.Name;
                var type = (IdentifierNameSyntax)name.TypeArgumentList.Arguments[0];
                var entity = type.Identifier.ValueText;
                return entity;
            })
            .Collect();

        context.RegisterSourceOutput(provider, static (context, provider) =>
        {
            context.AddSource($"{GeneratedFileOptions.MethodName}Implementation.g.cs", SourceText.From($$"""
                {{GeneratedFileOptions.Header}}

                namespace {{GeneratedFileOptions.Namespace}};

                internal static partial class SqueelDbConnectionExtensions
                {
                    public static partial global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<T>> {{GeneratedFileOptions.MethodName}}<T>(
                        this global::Npgsql.NpgsqlConnection connection,
                        ref global::{{GeneratedFileOptions.Namespace}}.SqueelInterpolatedStringHandler query,
                        global::System.Threading.CancellationToken ct)
                    {
                        var sql = query.ToString('@');

                        using var command = connection.CreateCommand();
                        command.CommandText = sql;
                        return __Exec(command, query.Parameters, ct);

                        static async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<T>> __Exec(global::Npgsql.NpgsqlCommand command, global::System.Collections.Generic.IEnumerable<global::{{GeneratedFileOptions.Namespace}}.ParameterDescriptor> parameters, global::System.Threading.CancellationToken ct)
                        {
                            foreach (var pd in parameters)
                            {
                                var p = command.CreateParameter();
                                p.ParameterName = pd.Name;
                                p.Value = pd.Value;
                                command.Parameters.Add(p);
                            }

                            using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                            var parser = _parsers[typeof(T)];

                            var buffer = new global::System.Collections.Generic.List<T>();
                            while (await reader.ReadAsync(ct).ConfigureAwait(false))
                                buffer.Add((T)(await parser(reader).ConfigureAwait(false)));
                            return buffer;
                        }
                    }

                    private static readonly global::System.Collections.Generic.Dictionary<global::System.Type, global::System.Func<global::Npgsql.NpgsqlDataReader, global::System.Threading.Tasks.Task<object>>> _parsers = new()
                    {
                {{string.Join("\n", provider.Select(e => $"        {CallSiteToDictionaryEntry(e)}"))}}
                    };
                }
                """, Encoding.UTF8));
        }); ;
    }

    private static string CallSiteToDictionaryEntry(string entity)
    {
        return $"[typeof(global::{GeneratedFileOptions.Namespace}.{entity})] = _parser{entity},";
    }
}
