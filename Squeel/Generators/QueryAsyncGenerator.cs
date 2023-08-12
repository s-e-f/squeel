using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Npgsql;
using Npgsql.Schema;
using Squeel.Diagnostics;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Data;

namespace Squeel.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class QueryAsyncGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entityProvider = context.SyntaxProvider
            .ForCallsToSqueelQueryMethod()
            .Combine(context.AnalyzerConfigOptionsProvider.ForSqueelConnectionString())
            .Combine(context.CompilationProvider)
            .Select(static (info, ct) =>
            {
                var argument = info.Left.Left.Invocation.ArgumentList.Arguments[0];
                var interpolatedQuery = (InterpolatedStringExpressionSyntax)argument.Expression;
                var sql = interpolatedQuery.ToParameterizedString(out var parameters);
                var member = (MemberAccessExpressionSyntax)info.Left.Left.Invocation.Expression;
                var name = (GenericNameSyntax)member.Name;
                var type = (IdentifierNameSyntax)name.TypeArgumentList.Arguments[0];
                var entity = type.Identifier.ValueText;
                var p = parameters.Select(p => new SqlParameterDescriptor(p.Key, info.Left.Left.SemanticModel.GetTypeInfo(p.Value).ToExampleValue())).ToImmutableArray();
                return new EntityDescriptor
                {
                    ConnectionString = info.Left.Right,
                    Name = entity,
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
            => x.Name == y.Name
            && x.Sql == y.Sql
            && x.Parameters.SequenceEqual(y.Parameters)
            && x.ConnectionString == y.ConnectionString
            && x.InterceptorPath == y.InterceptorPath
            && x.InterceptorLine == y.InterceptorLine
            && x.InterceptorColumn == y.InterceptorColumn;

        public int GetHashCode(EntityDescriptor obj) => HashCode.Combine(
            obj.Name,
            obj.Sql,
            obj.Parameters,
            obj.ConnectionString,
            obj.InterceptorPath,
            obj.InterceptorLine,
            obj.InterceptorColumn);
    }

    private readonly record struct EntityDescriptor
    {
        public required string Name { get; init; }
        public required string Sql { get; init; }
        public required ImmutableArray<SqlParameterDescriptor> Parameters { get; init; }
        public required string? ConnectionString { get; init; }
        public required string InterceptorPath { get; init; }
        public required int InterceptorLine { get; init; }
        public required int InterceptorColumn { get; init; }
        internal required Func<Location> GetLocation { get; init; }
    }

    private static ReadOnlyCollection<NpgsqlDbColumn> GetColumnSchema(
        NpgsqlConnection connection,
        string sql,
        in ImmutableArray<SqlParameterDescriptor> parameters)
    {
        using var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText = sql;
        foreach (var p in parameters)
        {
            schemaCommand.Parameters.Add(new NpgsqlParameter(p.Name, p.Value));
        }
        using var schemaReader = schemaCommand.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.SingleResult | CommandBehavior.KeyInfo);
        return schemaReader.GetColumnSchema();
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
                var columns = GetColumnSchema(connection, entity.Sql, entity.Parameters);

                GenerateEntity(context, entity, columns);
                GenerateInterceptor(context, entity, columns);
            }
            finally
            {
                transaction.Rollback();
            }
        }
        catch (PostgresException sql)
        {
            context.AddSource($"{entity.Name}.g.cs", $$"""
                {{GeneratedFileOptions.Header}}

                namespace {{GeneratedFileOptions.Namespace}};

                {{GeneratedFileOptions.Attribute}}
                internal sealed record {{entity.Name}}
                {
                    // Code omitted because SQL query failed
                    /*
                        {{sql.Message}}
                    */
                }
                """);

            context.ReportDiagnostic(Errors.QueryValidationFailed(entity.GetLocation(), "postgres", sql.MessageText));
        }
    }

    private static string ParseFromReader(NpgsqlDbColumn column)
    {
        var ordinality = column.ColumnOrdinal!.Value;
        var propertyName = column.ColumnName.Pascalize();
        var propertyType = column.DataType!.FullName;

        if (column.AllowDBNull is false)
            return $"{propertyName} = await reader.GetFieldValueAsync<global::{propertyType}>({ordinality}, ct).ConfigureAwait(false),";
        
        return $"{propertyName} = await reader.IsDBNullAsync({ordinality}, ct) ? null : await reader.GetFieldValueAsync<global::{propertyType}{Nullability(column)}>({ordinality}, ct).ConfigureAwait(false),";
    }

    private static string Nullability(NpgsqlDbColumn c)
    {
        if (c.AllowDBNull is true or null)
            return "?";
        else
            return string.Empty;
    }

    private static string Requiredness(NpgsqlDbColumn c)
    {
        if (c.DefaultValue is not null)
            return "";

        return " required";
    }

    private static string InterceptsLocationAttributeDefinition => $$"""
        namespace System.Runtime.CompilerServices
        {
        #pragma warning disable CS9113
            {{GeneratedFileOptions.Attribute}}
            [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]
            file sealed class InterceptsLocationAttribute(string filePath, int line, int character) : global::System.Attribute{}
        #pragma warning restore CS9113
        }
        """;

    static string JoinAndIndent(int indent, int skip, IEnumerable<string> lines)
    {
        return string.Join("\n", lines.Select((l, i) =>
        {
            if (i >= skip)
                return $"{new string(' ', indent)}{l}";
            else
                return l;
        }));
    }

    private static string GenerateEntityProperty(NpgsqlDbColumn c)
        => $"public{Requiredness(c)} global::{c.DataType!.FullName}{Nullability(c)} {c.ColumnName.Pascalize()} {{ get; init; }}";

    private static void GenerateEntity(in SourceProductionContext context, EntityDescriptor entity, ReadOnlyCollection<NpgsqlDbColumn> columns)
    {
        context.AddSource($"{entity.Name}.g.cs", $$""""
            {{GeneratedFileOptions.Header}}

            #nullable enable

            namespace {{GeneratedFileOptions.Namespace}};

            {{GeneratedFileOptions.Attribute}}
            internal sealed record {{entity.Name}}
            {
                {{JoinAndIndent(4, 1, columns.Select(GenerateEntityProperty))}}
            }
            """");
    }

    private static void GenerateInterceptor(in SourceProductionContext context, EntityDescriptor entity, ReadOnlyCollection<NpgsqlDbColumn> columns)
    {
        context.AddSource($"{entity.Name}Interceptor.g.cs", $$""""
            {{GeneratedFileOptions.Header}}

            #nullable enable

            {{InterceptsLocationAttributeDefinition}}

            namespace {{GeneratedFileOptions.Namespace}}
            {
                {{GeneratedFileOptions.Attribute}}
                internal static class {{entity.Name}}QueryImplementation
                {
                    {{GeneratedFileOptions.Attribute}}
                    [global::System.Runtime.CompilerServices.InterceptsLocation({{entity.InterceptorPath}}, {{entity.InterceptorLine}}, {{entity.InterceptorColumn}})]
                    public static global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<{{entity.Name}}>>
                        QueryAsync__{{entity.Name}}
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

                        static async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<{{entity.Name}}>>
                        __Exec
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

                            var flags = global::System.Data.CommandBehavior.SequentialAccess | global::System.Data.CommandBehavior.SingleResult;
                            using var reader = await command.ExecuteReaderAsync(flags, ct).ConfigureAwait(false);

                            var list = new global::System.Collections.Generic.List<{{entity.Name}}>();
                            while (await reader.ReadAsync(ct).ConfigureAwait(false))
                            {
                                list.Add(new {{entity.Name}}
                                {
                                    {{JoinAndIndent(24, 1, columns.Select(ParseFromReader))}}
                                });
                            }
                            return list;
                        }
                    }
                }
            }
            """");
    }
}
