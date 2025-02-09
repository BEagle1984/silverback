// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Messaging.Consuming.KafkaOffsetStore;

public class EntityFrameworkKafkaOffsetStoreSettingsFixture
{
    [Fact]
    public void Constructor_ShouldSetConnectionStringWithDefaultTableName()
    {
        EntityFrameworkKafkaOffsetStoreSettings settings = new(typeof(TestDbContext), GetDbContext);

        settings.DbContextType.ShouldBe(typeof(TestDbContext));
        settings.GetDbContext(Substitute.For<IServiceProvider>()).ShouldBeOfType<TestDbContext>();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        EntityFrameworkKafkaOffsetStoreSettings kafkaOffsetStoreSettings = new(typeof(TestDbContext), GetDbContext);

        Action act = kafkaOffsetStoreSettings.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbContextTypeIsNull()
    {
        EntityFrameworkKafkaOffsetStoreSettings settings = new(null!, GetDbContext);

        Action act = settings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The DbContext type is required.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbContextFactoryIsNull()
    {
        EntityFrameworkKafkaOffsetStoreSettings settings = new(typeof(TestDbContext), null!);

        Action act = settings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The DbContext factory is required.");
    }

    private static DbContext GetDbContext(IServiceProvider serviceProvider, ISilverbackContext? context = null) => new TestDbContext();

    private class TestDbContext : DbContext;
}
