// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
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

        implementationBuilder.ShouldBeOfType<PostgreSqlKafkaOffsetStoreSettingsBuilder>();
    }
}
