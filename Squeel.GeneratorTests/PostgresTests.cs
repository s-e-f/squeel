using Xunit.Abstractions;

namespace Squeel.GeneratorTests;

[Trait("Category", "Postgres")]
public sealed class PostgresTests : IClassFixture<PostgresContainer>
{
    private readonly PostgresContainer _postgres;
    private readonly ITestOutputHelper _output;

    public PostgresTests(PostgresContainer postgres, ITestOutputHelper output)
    {
        _postgres = postgres;
        _output = output;
    }

    [Fact(DisplayName = "Silent when unused")]
    public void SqueelShouldNotErrorWhenUnused()
    {
        var result = SqueelTestContext.Run(_postgres.ConnectionString, _output, """
            
            Console.WriteLine("Hello World");
            
            """);

        Assert.Multiple(
            () => Assert.Empty(result.GeneratorDiagnostics),
            () => Assert.Empty(result.Errors));
    }

    [Fact(DisplayName = "SELECT on complex type")]
    public void SqueelShouldWorkForSingleQueryWithNormalInterpolation()
    {
        var result = SqueelTestContext.Run(_postgres.ConnectionString, _output, $$"""
            using Npgsql;
            using Squeel;

            using var connection = new NpgsqlConnection("{{_postgres.ConnectionString}}");

            var email = "test@test.com";
            var users = connection.QueryAsync<User>($"SELECT email, date_of_birth, created FROM Users WHERE email = {email}");

            """);

        Assert.Multiple(
            () => Assert.Empty(result.GeneratorDiagnostics),
            () => Assert.Empty(result.Errors));
    }

    [Fact(DisplayName = "INSERT")]
    public void SqueelShouldWorkForSingleInsertWithNormalInterpolation()
    {
        var result = SqueelTestContext.Run(_postgres.ConnectionString, _output, $$"""
            using Npgsql;
            using Squeel;

            using var connection = new NpgsqlConnection("{{_postgres.ConnectionString}}");

            var email = "test@test.com";
            var dateOfBirth = DateTime.UtcNow;
            var affected = connection.ExecuteAsync($"INSERT INTO Users (email, date_of_birth) VALUES ({email}, {dateOfBirth})");

            """);

        Assert.Multiple(
            () => Assert.Empty(result.GeneratorDiagnostics),
            () => Assert.Empty(result.Errors));
    }

    [Fact(DisplayName = "UPDATE")]
    public void SqueelShouldWorkForSingleUpdateWithNormalInterpolation()
    {
        var result = SqueelTestContext.Run(_postgres.ConnectionString, _output, $$"""
            using Npgsql;
            using Squeel;

            using var connection = new NpgsqlConnection("{{_postgres.ConnectionString}}");

            var email = "test@test.com";
            var dateOfBirth = DateTime.UtcNow;
            var affected = connection.ExecuteAsync($"UPDATE users SET email = {email}, date_of_birth = {dateOfBirth}");

            """);

        Assert.Multiple(
            () => Assert.Empty(result.GeneratorDiagnostics),
            () => Assert.Empty(result.Errors));
    }

    [Fact(DisplayName = "DELETE")]
    public void SqueelShouldWorkForSingleDeleteWithNormalInterpolation()
    {
        var result = SqueelTestContext.Run(_postgres.ConnectionString, _output, $$"""
            using Npgsql;
            using Squeel;

            using var connection = new NpgsqlConnection("{{_postgres.ConnectionString}}");

            var email = "test@test.com";
            var affected = connection.ExecuteAsync($"DELETE FROM users WHERE email = {email}");

            """);

        Assert.Multiple(
            () => Assert.Empty(result.GeneratorDiagnostics),
            () => Assert.Empty(result.Errors));
    }

    [Fact(DisplayName = "SELECT invalid query generates one diagnostic")]
    public void FaultySqlShouldRaiseASingleGeneratorDiagnostic()
    {
        var result = SqueelTestContext.Run(_postgres.ConnectionString, _output, $$""""
            using Npgsql;
            using Squeel;

            using var connection = new NpgsqlConnection("{{_postgres.ConnectionString}}");

            var _ = await connection.QueryAsync<Faulty>($"""
                
                SELECT non_existent_column FROM non_existent_table

                """);

            """");

        Assert.Multiple(
            () => Assert.Empty(result.Errors),
            () => Assert.Single(result.GeneratorDiagnostics));
    }
}