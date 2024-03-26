// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Lock;

public class PostgreSqlTableLockSettingsBuilderFixture
{
    [Fact]
    public void Build_ShouldBuildSettings()
    {
        PostgreSqlTableLockSettingsBuilder builder = new("my-lock", "connection-string");

        DistributedLockSettings settings = builder.Build();

        settings.Should().BeOfType<PostgreSqlTableLockSettings>();
        settings.Should().BeEquivalentTo(new PostgreSqlTableLockSettings("my-lock", "connection-string"));
    }

    [Fact]
    public void WithTableName_ShouldSetLocksTableName()
    {
        PostgreSqlTableLockSettingsBuilder builder = new("my-lock", "connection-string");

        DistributedLockSettings settings = builder.WithTableName("test-locks").Build();

        settings.As<PostgreSqlTableLockSettings>().TableName.Should().Be("test-locks");
    }
}
