using Npgsql;

namespace Squeel.GeneratorTests;

public sealed class SqueelTests
(
)
{
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

        var email = $"{Guid.NewGuid()}@test.com";
        var dob = DateTimeOffset.UtcNow;

        var inserted = await connection.ExecuteAsync($"""
            INSERT INTO users (email, date_of_birth) VALUES ({email}, {dob})
            """);

        Assert.Equal(1, inserted);

        var newEmail = $"{Guid.NewGuid()}@test.com";

        var updated = await connection.ExecuteAsync($"""
            UPDATE users SET email = {newEmail} WHERE email = {email}
            """);

        Assert.Equal(1, updated);

        var users = connection.QueryAsync<User>($"""
            SELECT bio, email
            FROM users
            WHERE email = {newEmail}
            """);

        var user = await users.FirstAsync();

        Assert.Equal(newEmail, user.Email);

        var deleted = await connection.ExecuteAsync($"""
            DELETE FROM users WHERE email = {newEmail}
            """);

        Assert.Equal(1, deleted);
    }
}
