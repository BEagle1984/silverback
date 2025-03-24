// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.EntityFrameworkCore;
using Shouldly;
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

        settings.ShouldBeOfType<EntityFrameworkOutboxSettings>();
        settings.ShouldBe(new EntityFrameworkOutboxSettings(typeof(TestDbContext), GetDbContext));
    }

    private static DbContext GetDbContext(IServiceProvider serviceProvider, ISilverbackContext? context = null) => new TestDbContext();

    private class TestDbContext : DbContext;
}
