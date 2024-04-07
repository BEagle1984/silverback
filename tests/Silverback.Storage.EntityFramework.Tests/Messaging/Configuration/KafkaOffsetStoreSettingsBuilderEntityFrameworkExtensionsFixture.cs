// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Configuration;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Messaging.Configuration;

public class KafkaOffsetStoreSettingsBuilderEntityFrameworkExtensionsFixture
{
    [Fact]
    public void UseEntityFramework_ShouldReturnBuilder()
    {
        KafkaOffsetStoreSettingsBuilder builder = new();

        IKafkaOffsetStoreSettingsImplementationBuilder implementationBuilder = builder.UseEntityFramework<TestDbContext>();

        implementationBuilder.Should().BeOfType<EntityFrameworkKafkaOffsetStoreSettingsBuilder>();
    }

    private class TestDbContext : DbContext;
}
