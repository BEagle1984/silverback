// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Storage.Sqlite.Messaging.Configuration;

public class SqliteOutboxSettingsBuilderFixture
{
    [Fact]
    public void Build_ShouldBuildSettings()
    {
        SqliteOutboxSettingsBuilder builder = new("connection-string");

        OutboxSettings settings = builder.Build();

        settings.Should().BeOfType<SqliteOutboxSettings>();
        settings.Should().BeEquivalentTo(new SqliteOutboxSettings("connection-string"));
    }

    [Fact]
    public void WithName_ShouldSetOutboxName()
    {
        SqliteOutboxSettingsBuilder builder = new("connection-string");

        OutboxSettings settings = builder.WithTableName("test-outbox").Build();

        settings.As<SqliteOutboxSettings>().TableName.Should().Be("test-outbox");
    }
}
