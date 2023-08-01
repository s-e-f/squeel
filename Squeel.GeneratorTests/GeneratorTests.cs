using Microsoft.CodeAnalysis.CSharp;
using Xunit.Abstractions;

namespace Squeel.GeneratorTests;

public sealed class GeneratorTests : IClassFixture<PostgresContainer>
{
    private readonly PostgresContainer _postgres;
    private readonly ITestOutputHelper _output;

    public GeneratorTests(PostgresContainer postgres, ITestOutputHelper output)
    {
        _postgres = postgres;
        _output = output;
    }

    [Fact]
    public void SqueelShouldNotErrorWhenUnused()
    {
        var result = SqueelTestContext.Run(_postgres.ConnectionString, _output, CSharpSyntaxTree.ParseText("""
            
            Console.WriteLine("Hello World");
            
            """, path: "test-path/for-debugging/input.cs"));

        Assert.Multiple(
            () => Assert.Empty(result.GeneratorDiagnostics),
            () => Assert.Empty(result.Errors));
    }

    [Fact]
    public void SqueelShouldWorkForSingleUseWithNormalInterpolation()
    {
        var result = SqueelTestContext.Run(_postgres.ConnectionString, _output, CSharpSyntaxTree.ParseText($$"""
            using Npgsql;
            using Squeel;

            using var connection = new NpgsqlConnection("{{_postgres.ConnectionString}}");

            var email = "test@test.com";
            var users = connection.QueryAsync<User>($"SELECT email, date_of_birth FROM Users WHERE email = {email}", CancellationToken.None);

            """, path: "test-path/for-debugging/input.cs"));

        Assert.Multiple(
            () => Assert.Empty(result.GeneratorDiagnostics),
            () => Assert.Empty(result.Errors));
    }

    [Fact]
    public void SqueelShouldWorkForSingleUseWithRawStringLiteralInterpolation()
    {
        var result = SqueelTestContext.Run(_postgres.ConnectionString, _output, CSharpSyntaxTree.ParseText($$""""
            using Npgsql;
            using Squeel;

            using var connection = new NpgsqlConnection("{{_postgres.ConnectionString}}");

            var email = "test@test.com";
            var users = connection.QueryAsync<User>($"""

                SELECT email, date_of_birth FROM Users WHERE email = {email}
                
                """, CancellationToken.None);

            """", path: "test-path/for-debugging/input.cs"));

        Assert.Multiple(
            () => Assert.Empty(result.GeneratorDiagnostics),
            () => Assert.Empty(result.Errors));
    }

    [Fact]
    public void FaultySqlShouldRaiseASingleGeneratorDiagnostic()
    {
        var result = SqueelTestContext.Run(_postgres.ConnectionString, _output, CSharpSyntaxTree.ParseText($$""""
            using Npgsql;
            using Squeel;

            using var connection = new NpgsqlConnection("{{_postgres.ConnectionString}}");

            var _ = await connection.QueryAsync<Faulty>($"""
                
                SELECT non_existent_column FROM non_existent_table

                """, CancellationToken.None);

            """"));

        Assert.Multiple(
            () => Assert.Empty(result.Errors),
            () => Assert.Single(result.GeneratorDiagnostics));
    }
}