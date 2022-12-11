// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Storage.Sqlite.Messaging.Consuming.KafkaOffsetStore;

public class SqliteKafkaOffsetStoreSettingsFixture
{
    [Fact]
    public void Constructor_ShouldCreateDefaultSettings()
    {
        SqliteKafkaOffsetStoreSettings settings = new();

        settings.TableName.Should().Be("Silverback_KafkaOffsetStore");
    }

    [Fact]
    public void Constructor_ShouldSetConnectionStringAndTableName()
    {
        SqliteKafkaOffsetStoreSettings settings = new("connection-string", "my-offsets");

        settings.ConnectionString.Should().Be("connection-string");
        settings.TableName.Should().Be("my-offsets");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        SqliteKafkaOffsetStoreSettings kafkaOffsetStoreSettings = new("connection-string", "my-offsets");

        Action act = () => kafkaOffsetStoreSettings.Validate();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenTableNameIsNullOrWhitespace(string? tableName)
    {
        SqliteKafkaOffsetStoreSettings kafkaOffsetStoreSettings = new("connection-string") { TableName = tableName! };

        Action act = () => kafkaOffsetStoreSettings.Validate();

        act.Should().Throw<SilverbackConfigurationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenConnectionStringIsNullOrWhitespace(string? connectionString)
    {
        SqliteKafkaOffsetStoreSettings kafkaOffsetStoreSettings = new(connectionString!, "my-offsets");

        Action act = () => kafkaOffsetStoreSettings.Validate();

        act.Should().Throw<SilverbackConfigurationException>();
    }
}
