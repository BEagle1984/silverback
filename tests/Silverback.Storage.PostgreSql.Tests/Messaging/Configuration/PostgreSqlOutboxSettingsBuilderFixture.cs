// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Messaging.Configuration;

public class PostgreSqlOutboxSettingsBuilderFixture
{
    [Fact]
    public void Build_ShouldBuildSettings()
    {
        PostgreSqlOutboxSettingsBuilder builder = new("connection-string");

        OutboxSettings settings = builder.Build();

        settings.Should().BeOfType<PostgreSqlOutboxSettings>();
        settings.Should().BeEquivalentTo(new PostgreSqlOutboxSettings("connection-string"));
    }

    [Fact]
    public void WithTableName_ShouldSetOutboxTableName()
    {
        PostgreSqlOutboxSettingsBuilder builder = new("connection-string");

        OutboxSettings settings = builder.WithTableName("test-outbox").Build();

        settings.As<PostgreSqlOutboxSettings>().TableName.Should().Be("test-outbox");
    }
}
