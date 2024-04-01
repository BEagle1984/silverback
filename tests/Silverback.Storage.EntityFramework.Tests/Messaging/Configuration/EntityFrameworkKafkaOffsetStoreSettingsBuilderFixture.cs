// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Messaging.Configuration;

public class EntityFrameworkKafkaOffsetStoreSettingsBuilderFixture
{
    [Fact]
    public void Build_ShouldBuildDefaultSettings()
    {
        EntityFrameworkKafkaOffsetStoreSettingsBuilder builder = new(typeof(TestDbContext), GetDbContext);

        KafkaOffsetStoreSettings settings = builder.Build();

        settings.Should().BeOfType<EntityFrameworkKafkaOffsetStoreSettings>();
        settings.Should().BeEquivalentTo(new EntityFrameworkKafkaOffsetStoreSettings(typeof(TestDbContext), GetDbContext));
    }

    private static DbContext GetDbContext(IServiceProvider serviceProvider, SilverbackContext? context = null) => new TestDbContext();

    private class TestDbContext : DbContext;
}
