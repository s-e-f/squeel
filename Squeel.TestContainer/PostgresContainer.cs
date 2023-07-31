using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Squeel.TestContainers;

public sealed class PostgresContainer : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public string ConnectionString => _container.GetConnectionString();

    public PostgresContainer()
    {
        _container = new PostgreSqlBuilder()
            .WithPassword("Squeel123!")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE Users(email varchar(312), date_of_birth date)";
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
    }
}
