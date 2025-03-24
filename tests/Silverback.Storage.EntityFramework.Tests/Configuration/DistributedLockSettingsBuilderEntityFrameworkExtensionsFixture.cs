// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.EntityFrameworkCore;
using Shouldly;
using Silverback.Configuration;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Configuration;

public class DistributedLockSettingsBuilderEntityFrameworkExtensionsFixture
{
    [Fact]
    public void UseDbContext_ShouldReturnBuilder()
    {
        DistributedLockSettingsBuilder builder = new();

        EntityFrameworkLockSettingsBuilder implementationBuilder = builder.UseDbContext<TestDbContext>("lock");

        implementationBuilder.ShouldBeOfType<EntityFrameworkLockSettingsBuilder>();
    }

    private class TestDbContext : DbContext;
}
