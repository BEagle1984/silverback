// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Configuration;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Messaging.Configuration;

public class OutboxSettingsBuilderEntityFrameworkExtensionsFixture
{
    [Fact]
    public void UseEntityFramework_ShouldReturnBuilder()
    {
        OutboxSettingsBuilder builder = new();

        IOutboxSettingsImplementationBuilder implementationBuilder = builder.UseEntityFramework<TestDbContext>();

        implementationBuilder.Should().BeOfType<EntityFrameworkOutboxSettingsBuilder>();
    }

    private class TestDbContext : DbContext;
}
