# Squeel - What is it?

Squeel is a source generator that automatically validates your SQL queries and generates your POCO's.

Squeel is also the correct pronunciation of SQL.

## Security

Squeel uses string interpolation to simplify parameters. This is completely safe with Squeel through the `SqueelInterpolatedStringHandler`. This special interpolated string handler replaces interpolation holes with parameters, keeping your SQL queries safe. No SQL injections in Squeel!

## Type Safety

Squeel validates queries at build time, so if your code compiles, you KNOW your SQL queries are correct. It also generates the C# POCO's automatically so you KNOW the mappings are correct. The validation occurs by running the queries in a transaction that is immediately rolled back, so no mutations occur on your databases.

## Ease of Use

With Squeel you no longer have to write all that pesky boilerplate. This includes the POCO's, DTO's, Entities or whatever you want to call them. Squeel does it for you. In addition to validating the SQL query at build time, it uses the retrieved schema information to generate a correctly shaped C# record, and handles the parameter passing and reader/parser nonsense for you.

## Correctness

Squeel helps you in writing correct SQL queries by essentially doing the integration testing automatically at build time, without having to write any tests for the SQL to C# mappings manually.

## Simplicity

Squeel aims to simplify writing SQL queries and managing your database with SQL. It is **NOT** an ORM (object relational mapper) and never will be.

## How does it work?

Squeel requires a local database it can use to validate your SQL queries. Provide it by setting the `SqueelConnectionString` MSBuild property in your `.csproj` file.

> Due to a technical limitation in MSBuild and C# source generators, the connection string can **NOT** contain any semicolons (`;`). To work around this, substitute all semicolons for plus (`+`) symbols.

```xml
<Project>

    <PropertyGroup>
        <SqueelConnectionString>Host=localhost+User ID=postgres+Password=secretpassword+Database=mydatabase</SqueelConnectionString>
    </PropertyGroup>

</Project>
```

Squeel also generates as much boilerplate ADO.NET code as possible, turning this

```csharp
record Product(string Sku, string Description, decimal Price);

async Task<IEnumerable<Product>> GetProductsBelowPrice(this DbConnection connection, decimal maxPrice)
{
    using var command = connection.CreateCommand();
    command.CommandText = "SELECT sku, description, price FROM products WHERE price < @max_price";
    command.Parameters.Add(new NpgsqlParameter("max_price", maxPrice));
    using var reader = await command.ExecuteReaderAsync();
    var products = new List<Product>();
    while (await reader.ReadAsync())
    {
        products.Add(new Product(
            Sku: reader.GetString(0),
            Description: reader.GetString(1),
            Price: reader.GetDecimal(2)
        ));
    }
    return products;
}
```

Into this

```csharp
async Task<IEnumerable<Product>> GetProductsBelowPrice(this DbConnection connection, decimal maxPrice)
{
    return connection.QueryAsync<Product>($"""
        SELECT sku, description, price
        FROM products
        WHERE price < {maxPrice}
        """);    
}
```
