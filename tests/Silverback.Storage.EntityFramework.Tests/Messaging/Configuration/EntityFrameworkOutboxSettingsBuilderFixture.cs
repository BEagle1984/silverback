// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Messaging.Configuration;

public class EntityFrameworkOutboxSettingsBuilderFixture
{
    [Fact]
    public void Build_ShouldBuildSettings()
    {
        EntityFrameworkOutboxSettingsBuilder builder = new(typeof(TestDbContext), GetDbContext);

        OutboxSettings settings = builder.Build();

        settings.Should().BeOfType<EntityFrameworkOutboxSettings>();
        settings.Should().BeEquivalentTo(new EntityFrameworkOutboxSettings(typeof(TestDbContext), GetDbContext));
    }

    private static DbContext GetDbContext(IServiceProvider serviceProvider, SilverbackContext? context = null) => new TestDbContext();

    private class TestDbContext : DbContext
    {
    }
}
