using Npgsql;

namespace Squeel.GeneratorTests;

[Trait("Category", "Postgres")]
public sealed class SqueelTests
(
)
{
    private static NpgsqlConnection CreateConnection()
    {
        var connection = new NpgsqlConnection("Host=localhost+Port=5432+Password=squeel+User ID=postgres".Replace('+', ';'));
        connection.Open();
        return connection;
    }

    [Fact(DisplayName = "Happy flow")]
    public async Task QueryingUsersOnEmptyDatabaseYieldsEmptyEnumerable()
    {
        using var connection = CreateConnection();

        var email = $"{Guid.NewGuid()}@test.com";
        var dob = DateTime.UtcNow;

        var inserted = await connection.ExecuteAsync($"""
            INSERT INTO users (email, date_of_birth) VALUES ({email}, {dob})
            """);

        Assert.Equal(1, inserted);

        var newEmail = $"{Guid.NewGuid()}@test.com";

        var updated = await connection.ExecuteAsync($"""
            UPDATE users SET email = {newEmail} WHERE email = {email}
            """);

        Assert.Equal(1, updated);

        var users = await connection.QueryAsync<User>($"""
            SELECT bio, email
            FROM users
            WHERE email = {newEmail}
            """);

        Assert.Equal(newEmail, users.First().Email);

        var deleted = await connection.ExecuteAsync($"""
            DELETE FROM users WHERE email = {newEmail}
            """);

        Assert.Equal(1, deleted);
    }
}
