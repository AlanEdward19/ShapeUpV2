namespace IntegrationTests.Infrastructure;

/// <summary>
/// Test collection for read operations (SELECT only).
/// Tests in this collection should NOT modify data.
/// Uses the same fixture state from write tests.
/// </summary>
[CollectionDefinition("SQL Server Read Operations", DisableParallelization = true)]
public class SqlServerReadCollection : ICollectionFixture<SqlServerFixture>
{
}

