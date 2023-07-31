using Microsoft.CodeAnalysis.CSharp;
using Squeel.TestContainers;
using Xunit.Abstractions;

namespace Squeel.UnitTests;

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
    public void Test()
    {
        var result = SqueelTestContext.Run(_postgres.ConnectionString, _output, CSharpSyntaxTree.ParseText($$""""
            using Npgsql;
            using Squeel;

            await SqueelTests.QueryingUsersOnEmptyDatabaseYieldsEmptyEnumerable();

            static class SqueelTests
            {
                public static async Task QueryingUsersOnEmptyDatabaseYieldsEmptyEnumerable()
                {
                    using var connection = new NpgsqlConnection("{{_postgres.ConnectionString}}");

                    await connection.OpenAsync();

                    var users = await connection.QueryAsync<User>($"""
                        SELECT email, date_of_birth FROM Users
                        """, CancellationToken.None);
                }
            }
            """"));

        Assert.Multiple(
            () => Assert.Empty(result.GeneratorDiagnostics),
            () => Assert.Empty(result.Errors));
    }
}