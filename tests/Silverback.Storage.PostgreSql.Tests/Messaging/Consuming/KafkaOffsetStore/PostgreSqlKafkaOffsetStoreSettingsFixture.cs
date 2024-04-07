// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Messaging.Consuming.KafkaOffsetStore;

public class PostgreSqlKafkaOffsetStoreSettingsFixture
{
    [Fact]
    public void Constructor_ShouldSetConnectionStringWithDefaultTableName()
    {
        PostgreSqlKafkaOffsetStoreSettings settings = new("connection-string");

        settings.ConnectionString.Should().Be("connection-string");
        settings.TableName.Should().Be("SilverbackKafkaOffsets");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        PostgreSqlKafkaOffsetStoreSettings settings = new("connection-string");

        Action act = settings.Validate;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenTableNameIsNullOrWhitespace(string? tableName)
    {
        PostgreSqlKafkaOffsetStoreSettings settings = new("connection-string")
        {
            TableName = tableName!
        };

        Action act = settings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The kafkaOffsetStore table name is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenConnectionStringIsNullOrWhitespace(string? connectionString)
    {
        PostgreSqlKafkaOffsetStoreSettings settings = new(connectionString!);

        Action act = settings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The connection string is required.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbCommandTimeoutIsZero()
    {
        PostgreSqlKafkaOffsetStoreSettings outboxSettings = new("connection-string")
        {
            DbCommandTimeout = TimeSpan.Zero
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The command timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbCommandTimeoutIsLessThanZero()
    {
        PostgreSqlKafkaOffsetStoreSettings outboxSettings = new("connection-string")
        {
            DbCommandTimeout = TimeSpan.FromSeconds(-1)
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The command timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenCreateTableTimeoutIsZero()
    {
        PostgreSqlKafkaOffsetStoreSettings outboxSettings = new("connection-string")
        {
            CreateTableTimeout = TimeSpan.Zero
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The create table timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenCreateTableTimeoutIsLessThanZero()
    {
        PostgreSqlKafkaOffsetStoreSettings outboxSettings = new("connection-string")
        {
            CreateTableTimeout = TimeSpan.FromSeconds(-1)
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The create table timeout must be greater than zero.");
    }
}
