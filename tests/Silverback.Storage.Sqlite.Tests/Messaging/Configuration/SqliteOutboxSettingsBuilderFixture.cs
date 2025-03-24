// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
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

        settings.ShouldBe(new SqliteOutboxSettings("connection-string"));
    }

    [Fact]
    public void UseTable_ShouldSetOutboxTableName()
    {
        SqliteOutboxSettingsBuilder builder = new("connection-string");

        OutboxSettings settings = builder.UseTable("test-outbox").Build();

        settings.ShouldBeOfType<SqliteOutboxSettings>().TableName.ShouldBe("test-outbox");
    }

    [Fact]
    public void WithDbCommandTimeout_ShouldSetDbCommandTimeout()
    {
        SqliteOutboxSettingsBuilder builder = new("connection-string");

        OutboxSettings settings = builder.WithDbCommandTimeout(TimeSpan.FromSeconds(20)).Build();

        settings.ShouldBeOfType<SqliteOutboxSettings>().DbCommandTimeout.ShouldBe(TimeSpan.FromSeconds(20));
    }

    [Fact]
    public void WithCreateTableTimeout_ShouldSetCreateTableTimeout()
    {
        SqliteOutboxSettingsBuilder builder = new("connection-string");

        OutboxSettings settings = builder.WithCreateTableTimeout(TimeSpan.FromSeconds(40)).Build();

        settings.ShouldBeOfType<SqliteOutboxSettings>().CreateTableTimeout.ShouldBe(TimeSpan.FromSeconds(40));
    }
}
