// Copyright (c) 2023 Sergio Aquilini
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
        settings.TableName.Should().Be("Silverback_KafkaOffsetStore");
    }

    [Fact]
    public void Constructor_ShouldSetConnectionStringAndTableName()
    {
        PostgreSqlKafkaOffsetStoreSettings settings = new("connection-string", "my-offsets");

        settings.ConnectionString.Should().Be("connection-string");
        settings.TableName.Should().Be("my-offsets");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        PostgreSqlKafkaOffsetStoreSettings kafkaOffsetStoreSettings = new("connection-string", "my-offsets");

        Action act = kafkaOffsetStoreSettings.Validate;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenTableNameIsEmptyOrWhitespace(string tableName)
    {
        PostgreSqlKafkaOffsetStoreSettings kafkaOffsetStoreSettings = new("connection-string", tableName);

        Action act = kafkaOffsetStoreSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenConnectionStringIsNullOrWhitespace(string? connectionString)
    {
        PostgreSqlKafkaOffsetStoreSettings kafkaOffsetStoreSettings = new(connectionString!, "my-offsets");

        Action act = kafkaOffsetStoreSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>();
    }
}
