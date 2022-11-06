// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
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

        settings.Should().BeOfType<SqliteKafkaOffsetStoreSettings>();
        settings.Should().BeEquivalentTo(new SqliteKafkaOffsetStoreSettings("connection-string"));
    }

    [Fact]
    public void WithTableName_ShouldSetOffsetStoreTableName()
    {
        SqliteKafkaOffsetStoreSettingsBuilder builder = new("connection-string");

        KafkaOffsetStoreSettings settings = builder.WithTableName("test-offsetStore").Build();

        settings.As<SqliteKafkaOffsetStoreSettings>().TableName.Should().Be("test-offsetStore");
    }
}
