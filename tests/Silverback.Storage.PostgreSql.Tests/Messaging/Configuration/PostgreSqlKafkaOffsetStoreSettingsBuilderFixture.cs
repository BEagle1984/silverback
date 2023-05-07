// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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
}
