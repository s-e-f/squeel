using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Npgsql;
using Squeel.Diagnostics;
using System.Collections.Immutable;
using System.Data;

namespace Squeel.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class ExecuteAsyncGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entityProvider = context.SyntaxProvider
            .ForCallsToSqueelExecuteMethod()
            .Combine(context.AnalyzerConfigOptionsProvider.ForSqueelConnectionString())
            .Combine(context.CompilationProvider)
            .Select(static (info, ct) =>
            {
                var argument = info.Left.Left.Invocation.ArgumentList.Arguments[0];
                var interpolatedQuery = (InterpolatedStringExpressionSyntax)argument.Expression;
                var sql = interpolatedQuery.ToParameterizedString(out var parameters);
                var member = (MemberAccessExpressionSyntax)info.Left.Left.Invocation.Expression;
                var p = parameters.Select(p => new SqlParameterDescriptor(p.Key, info.Left.Left.SemanticModel.GetTypeInfo(p.Value).ToExampleValue())).ToImmutableArray();
                return new EntityDescriptor
                {
                    ConnectionString = info.Left.Right,
                    Sql = sql,
                    Parameters = p,
                    GetLocation = () => interpolatedQuery.SyntaxTree.GetLocation(interpolatedQuery.Span),
                    InterceptorPath = $"@\"{info.Right.Options.SourceReferenceResolver?.NormalizePath(member.SyntaxTree.FilePath, baseFilePath: null) ?? member.SyntaxTree.FilePath}\"",
                    InterceptorLine = member.Name.GetLocation().GetLineSpan().Span.Start.Line + 1,
                    InterceptorColumn = member.Name.GetLocation().GetLineSpan().Span.Start.Character + 1,
                };
            })
            .WithComparer(new EntityDescriptorComparer())
            ;

        context.RegisterSourceOutput(entityProvider, Generate);
    }

    private readonly record struct SqlParameterDescriptor(string Name, object? Value);

    private sealed class EntityDescriptorComparer : IEqualityComparer<EntityDescriptor>
    {
        public bool Equals(EntityDescriptor x, EntityDescriptor y)
            => x.Sql == y.Sql
            && x.Parameters.SequenceEqual(y.Parameters)
            && x.ConnectionString == y.ConnectionString
            && x.InterceptorPath == y.InterceptorPath
            && x.InterceptorLine == y.InterceptorLine
            && x.InterceptorColumn == y.InterceptorColumn;

        public int GetHashCode(EntityDescriptor obj) => HashCode.Combine(
            obj.Sql, 
            obj.Parameters, 
            obj.ConnectionString,
            obj.InterceptorPath,
            obj.InterceptorLine,
            obj.InterceptorColumn);
    }

    private readonly record struct EntityDescriptor
    {
        public required string Sql { get; init; }
        public required ImmutableArray<SqlParameterDescriptor> Parameters { get; init; }
        public required string? ConnectionString { get; init; }
        public required string InterceptorPath { get; init; }
        public required int InterceptorLine { get; init; }
        public required int InterceptorColumn { get; init; }
        internal required Func<Location> GetLocation { get; init; }
    }

    private static void Generate(SourceProductionContext context, EntityDescriptor entity)
    {
        try
        {
            using var connection = new NpgsqlConnection(entity.ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                using var schemaCommand = connection.CreateCommand();
                schemaCommand.CommandText = entity.Sql;
                foreach (var p in entity.Parameters)
                {
                    schemaCommand.Parameters.Add(new NpgsqlParameter(p.Name, p.Value));
                }
                using var schemaReader = schemaCommand.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.SingleResult);
                var columns = schemaReader.GetColumnSchema();
            }
            finally
            {
                transaction.Rollback();
            }

            context.AddSource($"Execute-{Guid.NewGuid()}.g.cs", $$"""
                {{GeneratedFileOptions.Header}}

                namespace System.Runtime.CompilerServices
                {
                #pragma warning disable CS9113
                    {{GeneratedFileOptions.Attribute}}
                    [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]
                    file sealed class InterceptsLocationAttribute(string filePath, int line, int character) : global::System.Attribute{}
                #pragma warning restore CS9113
                }

                namespace {{GeneratedFileOptions.Namespace}}
                {
                    {{GeneratedFileOptions.Attribute}}
                    file static class ExecuteImplementation
                    {
                        {{GeneratedFileOptions.Attribute}}
                        [global::System.Runtime.CompilerServices.InterceptsLocation({{entity.InterceptorPath}}, {{entity.InterceptorLine}}, {{entity.InterceptorColumn}})]
                        public static global::System.Threading.Tasks.Task<int>
                            ExecuteAsync
                        (
                            this global::Npgsql.NpgsqlConnection connection,
                            ref global::Squeel.SqueelInterpolatedStringHandler query,
                            global::System.Threading.CancellationToken ct = default
                        )
                        {
                            var sql = query.ToString('@');
                
                            using var command = connection.CreateCommand();
                            command.CommandText = sql;
                            return __Exec(command, query.Parameters, ct);
                
                            static async global::System.Threading.Tasks.Task<int> __Exec
                            (
                                global::Npgsql.NpgsqlCommand command,
                                global::System.Collections.Generic.IEnumerable<global::{{GeneratedFileOptions.Namespace}}.ParameterDescriptor> parameters,
                                global::System.Threading.CancellationToken ct
                            )
                            {
                                foreach (var pd in parameters)
                                {
                                    var p = command.CreateParameter();
                                    p.ParameterName = pd.Name;
                                    p.Value = pd.Value;
                                    command.Parameters.Add(p);
                                }
                
                                return await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                            }
                        }
                    }
                }
                """);
        }
        catch (PostgresException sql)
        {
            context.ReportDiagnostic(Errors.QueryValidationFailed(entity.GetLocation(), "postgres", sql.MessageText));
        }
    }
}
