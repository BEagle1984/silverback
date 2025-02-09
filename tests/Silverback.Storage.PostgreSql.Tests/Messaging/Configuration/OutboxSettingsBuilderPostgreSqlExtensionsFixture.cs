// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Configuration;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Messaging.Configuration;

public class OutboxSettingsBuilderPostgreSqlExtensionsFixture
{
    [Fact]
    public void UsePostgreSql_ShouldReturnBuilder()
    {
        OutboxSettingsBuilder builder = new();

        IOutboxSettingsImplementationBuilder implementationBuilder = builder.UsePostgreSql("connection-string");

        implementationBuilder.ShouldBeOfType<PostgreSqlOutboxSettingsBuilder>();
    }
}
