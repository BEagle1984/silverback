// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Lock;

public class PostgreSqlAdvisoryLockSettingsBuilderFixture
{
    [Fact]
    public void Build_ShouldBuildSettings()
    {
        PostgreSqlAdvisoryLockSettingsBuilder builder = new("my-lock", "connection-string");

        DistributedLockSettings settings = builder.Build();

        settings.Should().BeOfType<PostgreSqlAdvisoryLockSettings>();
        settings.Should().BeEquivalentTo(new PostgreSqlAdvisoryLockSettings("my-lock", "connection-string"));
    }
}
