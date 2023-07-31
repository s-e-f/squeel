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

    [Fact]
    public async Task QueryingUsersOnEmptyDatabaseYieldsEmptyEnumerable()
    {
        using var connection = new NpgsqlConnection("Host=localhost+Port=5432+Password=P@ssw0rd+Database=squeel+User ID=postgres".Replace('+', ';'));

        await connection.OpenAsync();

        var email = "test@test.com";

        var users = await connection.QueryAsync<User>($"""
            SELECT email, date_of_birth, score FROM Users WHERE email = {email}
            """, CancellationToken.None);

        Assert.NotNull(users);
        Assert.NotEmpty(users);
    }
}
