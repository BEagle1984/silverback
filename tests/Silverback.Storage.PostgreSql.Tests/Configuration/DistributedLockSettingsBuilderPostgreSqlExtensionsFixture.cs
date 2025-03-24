// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Configuration;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Configuration;

public class DistributedLockSettingsBuilderPostgreSqlExtensionsFixture
{
    [Fact]
    public void UsePostgreSqlAdvisoryLock_ShouldReturnBuilder()
    {
        DistributedLockSettingsBuilder builder = new();

        PostgreSqlAdvisoryLockSettingsBuilder result = builder.UsePostgreSqlAdvisoryLock("lock", "conn");

        result.ShouldBeOfType<PostgreSqlAdvisoryLockSettingsBuilder>();
    }

    [Fact]
    public void UsePostgreSqlTable_ShouldReturnBuilder()
    {
        DistributedLockSettingsBuilder builder = new();

        PostgreSqlTableLockSettingsBuilder result = builder.UsePostgreSqlTable("lock", "conn");

        result.ShouldBeOfType<PostgreSqlTableLockSettingsBuilder>();
    }
}
