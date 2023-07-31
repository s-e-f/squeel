using Npgsql;
using Xunit.Abstractions;
using Squeel;
using Squeel.TestContainers;

namespace Squeel.GeneratorTests;

public sealed class SqueelTests : IClassFixture<PostgresContainer>
{
    private readonly PostgresContainer _container;
    private readonly ITestOutputHelper _output;

    public SqueelTests(PostgresContainer container, ITestOutputHelper output)
    {
        _container = container;
        _output = output;
    }

    private static NpgsqlConnection CreateConnection()
    {
        var connection = new NpgsqlConnection("Host=localhost+Port=5432+Password=P@ssw0rd+Database=squeel+User ID=postgres".Replace('+', ';'));
        connection.Open();
        return connection;
    }

    [Fact]
    public async Task QueryingUsersOnEmptyDatabaseYieldsEmptyEnumerable()
    {
        using var connection = CreateConnection();
        
        var email = "test@test.com";

        var users = await connection.QueryAsync<User>($"""
            SELECT email, date_of_birth, score
            FROM users
            WHERE email = {email}
            """, CancellationToken.None);

        foreach (var user in users)
        {
            _output.WriteLine(user.Email);
        }
    }
}
