// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Configuration;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Configuration;

public class KafkaOffsetStoreSettingsBuilderMemoryExtensionsFixture
{
    [Fact]
    public void UseMemory_ShouldReturnBuilder()
    {
        KafkaOffsetStoreSettingsBuilder builder = new();

        IKafkaOffsetStoreSettingsImplementationBuilder implementationBuilder = builder.UseMemory();

        implementationBuilder.ShouldBeOfType<InMemoryKafkaOffsetStoreSettingsBuilder>();
    }
}
