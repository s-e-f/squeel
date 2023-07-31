using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Npgsql;
using Npgsql.Schema;
using System.Collections.Immutable;
using System.Data;
using System.Text;

namespace Squeel;

[Generator(LanguageNames.CSharp)]
public sealed class EntityGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entityProvider = context.SyntaxProvider
            .ForCallsToSqueelQueryMethod()
            .Combine(context.AnalyzerConfigOptionsProvider.ForSqueelConnectionString())
            .Select(static (info, ct) =>
            {
                var argument = info.Left.Invocation.ArgumentList.Arguments[0];
                var interpolatedQuery = (InterpolatedStringExpressionSyntax)argument.Expression;
                var sql = interpolatedQuery.ToParameterizedString(out var parameters);
                var member = (MemberAccessExpressionSyntax)info.Left.Invocation.Expression;
                var name = (GenericNameSyntax)member.Name;
                var type = (IdentifierNameSyntax)name.TypeArgumentList.Arguments[0];
                var entity = type.Identifier.ValueText;
                var p = parameters.Select(p => new SqlParameterDescriptor(p.Key, info.Left.SemanticModel.GetTypeInfo(p.Value).ToRandomizedValue())).ToImmutableArray();
                return new EntityDescriptor
                {
                    ConnectionString = info.Right,
                    Name = entity,
                    Sql = sql,
                    Parameters = p,
                };
            })
            ;

        context.RegisterSourceOutput(entityProvider, Generate);
    }

    private readonly record struct SqlParameterDescriptor(string Name, object? Value);

    private readonly record struct EntityDescriptor
    {
        public required string Name { get; init; }
        public required string Sql { get; init; }
        public required ImmutableArray<SqlParameterDescriptor> Parameters { get; init; }
        public required string? ConnectionString { get; init; }
    }

    private static void Generate(SourceProductionContext context, EntityDescriptor entity)
    {
        using var connection = new NpgsqlConnection(entity.ConnectionString);
        connection.Open();
        using var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText = entity.Sql;
        foreach (var p in entity.Parameters)
        {
            schemaCommand.Parameters.Add(new NpgsqlParameter(p.Name, p.Value));
        }
        using var schemaReader = schemaCommand.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.SingleResult);
        var columns = schemaReader.GetColumnSchema();

        context.AddSource($"{entity.Name}.g.cs", SourceText.From($$"""
            {{GeneratedFileOptions.Header}}

            namespace {{GeneratedFileOptions.Namespace}};

            {{GeneratedFileOptions.Attribute}}
            internal sealed record {{entity.Name}}
            {
            {{string.Join("\n", columns.Select(c => $"    public required global::{c.DataType!.FullName} {c.ColumnName.Pascalize()} {{ get; init; }}"))}}
            }

            internal static partial class SqueelDbConnectionExtensions
            {
                {{GeneratedFileOptions.Attribute}}
                private static readonly global::System.Func<global::Npgsql.NpgsqlDataReader, global::System.Threading.Tasks.Task<object>> _parser{{entity.Name}} = async reader =>
                {
                    return new global::{{GeneratedFileOptions.Namespace}}.{{entity.Name}}
                    {
            {{string.Join("\n", columns.Select(c => $"            {ParseFromReader(c)}"))}}            
                    };
                };
            }
            """, Encoding.UTF8));
    }

    private static string ParseFromReader(NpgsqlDbColumn column)
    {
        var ordinality = column.ColumnOrdinal!.Value;
        var propertyName = column.ColumnName.Pascalize();
        var propertyType = column.DataType!.FullName;

        return $"{propertyName} = await reader.GetFieldValueAsync<global::{propertyType}>({ordinality}).ConfigureAwait(false),";
    }
}
