// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Lock;

public class PostgreSqlAdvisoryLockSettingsBuilderTests
{
    [Fact]
    public void Build_ShouldBuildSettings()
    {
        PostgreSqlAdvisoryLockSettingsBuilder builder = new("my-lock", "connection-string");

        DistributedLockSettings settings = builder.Build();

        settings.ShouldBeOfType<PostgreSqlAdvisoryLockSettings>();
        settings.ShouldBe(new PostgreSqlAdvisoryLockSettings("my-lock", "connection-string"));
    }
}
