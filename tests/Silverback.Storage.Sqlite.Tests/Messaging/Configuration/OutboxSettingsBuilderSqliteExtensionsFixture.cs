// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Configuration;
using Xunit;

namespace Silverback.Tests.Storage.Sqlite.Messaging.Configuration;

public class OutboxSettingsBuilderSqliteExtensionsFixture
{
    [Fact]
    public void UseSqlite_ShouldReturnBuilder()
    {
        OutboxSettingsBuilder builder = new();

        IOutboxSettingsImplementationBuilder implementationBuilder = builder.UseSqlite("connection-string");

        implementationBuilder.ShouldBeOfType<SqliteOutboxSettingsBuilder>();
    }
}
