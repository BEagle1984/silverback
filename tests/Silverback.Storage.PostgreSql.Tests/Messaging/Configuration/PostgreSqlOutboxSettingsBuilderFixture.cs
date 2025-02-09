// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
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

        settings.ShouldBe(new PostgreSqlOutboxSettings("connection-string"));
    }

    [Fact]
    public void UseTable_ShouldSetOutboxTableName()
    {
        PostgreSqlOutboxSettingsBuilder builder = new("connection-string");

        OutboxSettings settings = builder.UseTable("test-outbox").Build();

        settings.ShouldBeOfType<PostgreSqlOutboxSettings>().TableName.ShouldBe("test-outbox");
    }

    [Fact]
    public void WithDbCommandTimeout_ShouldSetDbCommandTimeout()
    {
        PostgreSqlOutboxSettingsBuilder builder = new("connection-string");

        OutboxSettings settings = builder.WithDbCommandTimeout(TimeSpan.FromSeconds(20)).Build();

        settings.ShouldBeOfType<PostgreSqlOutboxSettings>().DbCommandTimeout.ShouldBe(TimeSpan.FromSeconds(20));
    }

    [Fact]
    public void WithCreateTableTimeout_ShouldSetCreateTableTimeout()
    {
        PostgreSqlOutboxSettingsBuilder builder = new("connection-string");

        OutboxSettings settings = builder.WithCreateTableTimeout(TimeSpan.FromSeconds(40)).Build();

        settings.ShouldBeOfType<PostgreSqlOutboxSettings>().CreateTableTimeout.ShouldBe(TimeSpan.FromSeconds(40));
    }
}
