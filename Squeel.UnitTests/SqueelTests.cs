using Microsoft.CodeAnalysis.CSharp;
using Testcontainers.PostgreSql;

namespace Squeel.UnitTests;

public sealed class PostgresContainer : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public string ConnectionString => _container.GetConnectionString();

    public PostgresContainer()
    {
        _container = new PostgreSqlBuilder().Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
    }
}

public sealed class SqueelTests : IClassFixture<PostgresContainer>
{
    private readonly PostgresContainer _postgres;

    public SqueelTests(PostgresContainer postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public void Test1()
    {
        var result = SqueelTestContext<QueryGenerator>.Run(_postgres.ConnectionString, CSharpSyntaxTree.ParseText("""
            
            Console.WriteLine("Hello World");
            
            """, path: "test-path/for-debugging/input.cs"));

        Assert.Empty(result.Errors);
        Assert.Empty(result.GeneratorDiagnostics);
    }
}