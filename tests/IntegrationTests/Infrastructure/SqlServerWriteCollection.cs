namespace IntegrationTests.Infrastructure;

/// <summary>
/// Test collection for write operations (INSERT, UPDATE, DELETE).
/// Tests in this collection can modify data and maintain state between tests.
/// Disable parallel execution to avoid test isolation issues.
/// </summary>
[CollectionDefinition("SQL Server Write Operations", DisableParallelization = true)]
public class SqlServerWriteCollection : ICollectionFixture<SqlServerFixture>
{
}

