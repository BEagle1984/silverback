// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Configuration;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Configuration;

public class OutboxSettingsBuilderMemoryExtensionsFixture
{
    [Fact]
    public void UseMemory_ShouldReturnBuilder()
    {
        OutboxSettingsBuilder builder = new();

        IOutboxSettingsImplementationBuilder implementationBuilder = builder.UseMemory();

        implementationBuilder.ShouldBeOfType<InMemoryOutboxSettingsBuilder>();
    }
}
