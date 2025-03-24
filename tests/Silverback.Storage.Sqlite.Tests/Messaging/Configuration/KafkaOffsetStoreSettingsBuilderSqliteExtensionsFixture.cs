// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Configuration;
using Xunit;

namespace Silverback.Tests.Storage.Sqlite.Messaging.Configuration;

public class KafkaOffsetStoreSettingsBuilderSqliteExtensionsFixture
{
    [Fact]
    public void UseSqlite_ShouldReturnBuilder()
    {
        KafkaOffsetStoreSettingsBuilder builder = new();

        IKafkaOffsetStoreSettingsImplementationBuilder implementationBuilder = builder.UseSqlite("connection-string");

        implementationBuilder.ShouldBeOfType<SqliteKafkaOffsetStoreSettingsBuilder>();
    }
}
