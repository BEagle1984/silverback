// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
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

        result.Should().BeOfType<PostgreSqlAdvisoryLockSettingsBuilder>();
    }

    [Fact]
    public void UsePostgreSqlTable_ShouldReturnBuilder()
    {
        DistributedLockSettingsBuilder builder = new();

        PostgreSqlTableLockSettingsBuilder result = builder.UsePostgreSqlTable("lock", "conn");

        result.Should().BeOfType<PostgreSqlTableLockSettingsBuilder>();
    }
}
