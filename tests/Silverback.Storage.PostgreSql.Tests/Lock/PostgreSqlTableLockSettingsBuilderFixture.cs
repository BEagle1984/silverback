// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
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

        settings.ShouldBeOfType<PostgreSqlTableLockSettings>();
        settings.ShouldBe(new PostgreSqlTableLockSettings("my-lock", "connection-string"));
    }

    [Fact]
    public void UseTable_ShouldSetLocksTableName()
    {
        PostgreSqlTableLockSettingsBuilder builder = new("my-lock", "connection-string");

        DistributedLockSettings settings = builder.UseTable("test-locks").Build();

        settings.ShouldBeOfType<PostgreSqlTableLockSettings>().TableName.ShouldBe("test-locks");
    }
}
