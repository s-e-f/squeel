using System.Data;

using Microsoft.CodeAnalysis;

using Npgsql;

namespace Squeel;

[Generator(LanguageNames.CSharp)]
public sealed class QueryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.AnalyzerConfigOptionsProvider, (context, config) =>
        {
            var output = config.GlobalOptions.TryGetValue("build_property.SqueelConnectionString", out var value)
                ? $"""
                    // Found connection string:
                    //
                    // '{value.Replace('+', ';')}'
                    """
                : null;

            if (value is null || output is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "SQUEEL0001",
                        title: "Missing required MSBuild property: SqueelConnectionString",
                        messageFormat: "Missing required MSBuild property: SqueelConnectionString",
                        category: "Squeel",
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    Location.None));

                return;
            }

            var connectionString = value.Replace('+', ';');
            var dataSource = NpgsqlDataSource.Create(connectionString);
            var connection = dataSource.OpenConnection();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            var reader = command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SingleResult | CommandBehavior.CloseConnection);
            var c = 0;  while (reader.Read()) { c++; }

            output += $"""
                Found {c} rows in the database from health check.
                """;

            context.AddSource("Squeel.g.cs", output);
        });
    }
}
