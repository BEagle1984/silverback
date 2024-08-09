// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Lock;

public class EntityFrameworkLockSettingsBuilderFixture
{
    [Fact]
    public void Build_ShouldBuildSettings()
    {
        static DbContext GetDbContext(IServiceProvider serviceProvider, ISilverbackContext? context = null) => null!;
        EntityFrameworkLockSettingsBuilder builder = new("my-lock", typeof(DbContext), GetDbContext);

        DistributedLockSettings settings = builder.Build();

        settings.Should().BeOfType<EntityFrameworkLockSettings>();
        settings.Should().BeEquivalentTo(new EntityFrameworkLockSettings("my-lock", typeof(DbContext), GetDbContext));
    }
}
