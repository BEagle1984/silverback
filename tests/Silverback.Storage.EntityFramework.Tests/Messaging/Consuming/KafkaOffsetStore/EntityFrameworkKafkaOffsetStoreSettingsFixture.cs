// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Messaging.Consuming.KafkaOffsetStore;

public class EntityFrameworkKafkaOffsetStoreSettingsFixture
{
    [Fact]
    public void Constructor_ShouldSetConnectionStringWithDefaultTableName()
    {
        EntityFrameworkKafkaOffsetStoreSettings settings = new(typeof(TestDbContext), GetDbContext);

        settings.DbContextType.Should().Be(typeof(TestDbContext));
        settings.GetDbContext(Substitute.For<IServiceProvider>()).Should().BeOfType<TestDbContext>();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        EntityFrameworkKafkaOffsetStoreSettings kafkaOffsetStoreSettings = new(typeof(TestDbContext), GetDbContext);

        Action act = kafkaOffsetStoreSettings.Validate;

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbContextTypeIsNull()
    {
        EntityFrameworkKafkaOffsetStoreSettings settings = new(null!, GetDbContext);

        Action act = settings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The DbContext type is required.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbContextFactoryIsNull()
    {
        EntityFrameworkKafkaOffsetStoreSettings settings = new(typeof(TestDbContext), null!);

        Action act = settings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The DbContext factory is required.");
    }

    private static DbContext GetDbContext(IServiceProvider serviceProvider, SilverbackContext? context = null) => new TestDbContext();

    private class TestDbContext : DbContext;
}
