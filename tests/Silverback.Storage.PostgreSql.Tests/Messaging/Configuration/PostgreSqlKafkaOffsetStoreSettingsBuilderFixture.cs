// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
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

        settings.Should().BeOfType<PostgreSqlKafkaOffsetStoreSettings>();
        settings.Should().BeEquivalentTo(new PostgreSqlKafkaOffsetStoreSettings("connection-string"));
    }

    [Fact]
    public void WithTableName_ShouldSetOffsetStoreTableName()
    {
        PostgreSqlKafkaOffsetStoreSettingsBuilder builder = new("connection-string");

        KafkaOffsetStoreSettings settings = builder.WithTableName("test-offsetStore").Build();

        settings.As<PostgreSqlKafkaOffsetStoreSettings>().TableName.Should().Be("test-offsetStore");
    }

    [Fact]
    public void WithDbCommandTimeout_ShouldSetDbCommandTimeout()
    {
        PostgreSqlKafkaOffsetStoreSettingsBuilder builder = new("connection-string");

        KafkaOffsetStoreSettings settings = builder.WithDbCommandTimeout(TimeSpan.FromSeconds(20)).Build();

        settings.As<PostgreSqlKafkaOffsetStoreSettings>().DbCommandTimeout.Should().Be(TimeSpan.FromSeconds(20));
    }

    [Fact]
    public void WithCreateTableTimeout_ShouldSetCreateTableTimeout()
    {
        PostgreSqlKafkaOffsetStoreSettingsBuilder builder = new("connection-string");

        KafkaOffsetStoreSettings settings = builder.WithCreateTableTimeout(TimeSpan.FromSeconds(40)).Build();

        settings.As<PostgreSqlKafkaOffsetStoreSettings>().CreateTableTimeout.Should().Be(TimeSpan.FromSeconds(40));
    }
}
