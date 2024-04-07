// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Messaging.Configuration;

public class KafkaOffsetStoreSettingsBuilderPostgreSqlExtensionsFixture
{
    [Fact]
    public void UsePostgreSql_ShouldReturnBuilder()
    {
        KafkaOffsetStoreSettingsBuilder builder = new();

        IKafkaOffsetStoreSettingsImplementationBuilder implementationBuilder = builder.UsePostgreSql("connection-string");

        implementationBuilder.Should().BeOfType<PostgreSqlKafkaOffsetStoreSettingsBuilder>();
    }
}
