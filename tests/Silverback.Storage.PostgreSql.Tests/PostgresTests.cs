// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Xunit;

namespace Silverback.Tests.Storage.PostgreSql;

[Trait("Dependency", "Docker")]
[Trait("Database", "PostgreSql")]
[Collection(nameof(PostgresCollection))]
public abstract class PostgresTests
{
    protected PostgresTests(PostgresContainerFixture postgresContainerFixture)
    {
        ConnectionString = postgresContainerFixture.GetNewConnectionString();
    }

    protected string ConnectionString { get; }
}
