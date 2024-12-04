// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
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
    public void UseTable_ShouldSetOutboxTableName()
    {
        SqliteOutboxSettingsBuilder builder = new("connection-string");

        OutboxSettings settings = builder.UseTable("test-outbox").Build();

        settings.As<SqliteOutboxSettings>().TableName.Should().Be("test-outbox");
    }

    [Fact]
    public void WithDbCommandTimeout_ShouldSetDbCommandTimeout()
    {
        SqliteOutboxSettingsBuilder builder = new("connection-string");

        OutboxSettings settings = builder.WithDbCommandTimeout(TimeSpan.FromSeconds(20)).Build();

        settings.As<SqliteOutboxSettings>().DbCommandTimeout.Should().Be(TimeSpan.FromSeconds(20));
    }

    [Fact]
    public void WithCreateTableTimeout_ShouldSetCreateTableTimeout()
    {
        SqliteOutboxSettingsBuilder builder = new("connection-string");

        OutboxSettings settings = builder.WithCreateTableTimeout(TimeSpan.FromSeconds(40)).Build();

        settings.As<SqliteOutboxSettings>().CreateTableTimeout.Should().Be(TimeSpan.FromSeconds(40));
    }
}
