namespace IntegrationTests.Infrastructure;

[CollectionDefinition("Integration SQL Server", DisableParallelization = true)]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
}

