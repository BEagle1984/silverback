// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Messaging.Configuration;

public class PostgreSqlKafkaOffsetStoreSettingsBuilderFixture
{
    [Fact]
    public void Build_ShouldBuildDefaultSettings()
    {
        PostgreSqlKafkaOffsetStoreSettingsBuilder builder = new("connection-string");

        KafkaOffsetStoreSettings settings = builder.Build();

        settings.ShouldBe(new PostgreSqlKafkaOffsetStoreSettings("connection-string"));
    }

    [Fact]
    public void UseTable_ShouldSetOffsetStoreTableName()
    {
        PostgreSqlKafkaOffsetStoreSettingsBuilder builder = new("connection-string");

        KafkaOffsetStoreSettings settings = builder.UseTable("test-offsetStore").Build();

        settings.ShouldBeOfType<PostgreSqlKafkaOffsetStoreSettings>().TableName.ShouldBe("test-offsetStore");
    }

    [Fact]
    public void WithDbCommandTimeout_ShouldSetDbCommandTimeout()
    {
        PostgreSqlKafkaOffsetStoreSettingsBuilder builder = new("connection-string");

        KafkaOffsetStoreSettings settings = builder.WithDbCommandTimeout(TimeSpan.FromSeconds(20)).Build();

        settings.ShouldBeOfType<PostgreSqlKafkaOffsetStoreSettings>().DbCommandTimeout.ShouldBe(TimeSpan.FromSeconds(20));
    }

    [Fact]
    public void WithCreateTableTimeout_ShouldSetCreateTableTimeout()
    {
        PostgreSqlKafkaOffsetStoreSettingsBuilder builder = new("connection-string");

        KafkaOffsetStoreSettings settings = builder.WithCreateTableTimeout(TimeSpan.FromSeconds(40)).Build();

        settings.ShouldBeOfType<PostgreSqlKafkaOffsetStoreSettings>().CreateTableTimeout.ShouldBe(TimeSpan.FromSeconds(40));
    }
}
