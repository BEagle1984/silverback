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
    public void Constructor_ShouldSetConnectionStringWithDefaultTableName()
    {
        SqliteKafkaOffsetStoreSettings settings = new("connection-string");

        settings.ConnectionString.Should().Be("connection-string");
        settings.TableName.Should().Be("SilverbackKafkaOffsets");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        SqliteKafkaOffsetStoreSettings kafkaOffsetStoreSettings = new("connection-string");

        Action act = kafkaOffsetStoreSettings.Validate;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenTableNameIsNullOrWhitespace(string? tableName)
    {
        SqliteKafkaOffsetStoreSettings kafkaOffsetStoreSettings = new("connection-string")
        {
            TableName = tableName!
        };

        Action act = kafkaOffsetStoreSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenConnectionStringIsNullOrWhitespace(string? connectionString)
    {
        SqliteKafkaOffsetStoreSettings kafkaOffsetStoreSettings = new(connectionString!);

        Action act = kafkaOffsetStoreSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbCommandTimeoutIsZero()
    {
        SqliteKafkaOffsetStoreSettings outboxSettings = new("connection-string")
        {
            DbCommandTimeout = TimeSpan.Zero
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The command timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbCommandTimeoutIsLessThanZero()
    {
        SqliteKafkaOffsetStoreSettings outboxSettings = new("connection-string")
        {
            DbCommandTimeout = TimeSpan.FromSeconds(-1)
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The command timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenCreateTableTimeoutIsZero()
    {
        SqliteKafkaOffsetStoreSettings outboxSettings = new("connection-string")
        {
            CreateTableTimeout = TimeSpan.Zero
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The create table timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenCreateTableTimeoutIsLessThanZero()
    {
        SqliteKafkaOffsetStoreSettings outboxSettings = new("connection-string")
        {
            CreateTableTimeout = TimeSpan.FromSeconds(-1)
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The create table timeout must be greater than zero.");
    }
}
