// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Storage.Sqlite.Messaging.Configuration;

public class SqliteKafkaOffsetStoreSettingsBuilderFixture
{
    [Fact]
    public void Build_ShouldBuildDefaultSettings()
    {
        SqliteKafkaOffsetStoreSettingsBuilder builder = new("connection-string");

        KafkaOffsetStoreSettings settings = builder.Build();

        settings.ShouldBe(new SqliteKafkaOffsetStoreSettings("connection-string"));
    }

    [Fact]
    public void UseTable_ShouldSetOffsetStoreTableName()
    {
        SqliteKafkaOffsetStoreSettingsBuilder builder = new("connection-string");

        KafkaOffsetStoreSettings settings = builder.UseTable("test-offsetStore").Build();

        settings.ShouldBeOfType<SqliteKafkaOffsetStoreSettings>().TableName.ShouldBe("test-offsetStore");
    }

    [Fact]
    public void WithDbCommandTimeout_ShouldSetDbCommandTimeout()
    {
        SqliteKafkaOffsetStoreSettingsBuilder builder = new("connection-string");

        KafkaOffsetStoreSettings settings = builder.WithDbCommandTimeout(TimeSpan.FromSeconds(20)).Build();

        settings.ShouldBeOfType<SqliteKafkaOffsetStoreSettings>().DbCommandTimeout.ShouldBe(TimeSpan.FromSeconds(20));
    }

    [Fact]
    public void WithCreateTableTimeout_ShouldSetCreateTableTimeout()
    {
        SqliteKafkaOffsetStoreSettingsBuilder builder = new("connection-string");

        KafkaOffsetStoreSettings settings = builder.WithCreateTableTimeout(TimeSpan.FromSeconds(40)).Build();

        settings.ShouldBeOfType<SqliteKafkaOffsetStoreSettings>().CreateTableTimeout.ShouldBe(TimeSpan.FromSeconds(40));
    }
}
