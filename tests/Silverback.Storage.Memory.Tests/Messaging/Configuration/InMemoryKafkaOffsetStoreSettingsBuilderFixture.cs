// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Configuration;

public class InMemoryKafkaOffsetStoreSettingsBuilderFixture
{
    [Fact]
    public void Build_ShouldBuildDefaultSettings()
    {
        InMemoryKafkaOffsetStoreSettingsBuilder builder = new();

        KafkaOffsetStoreSettings settings = builder.Build();

        settings.Should().BeOfType<InMemoryKafkaOffsetStoreSettings>();
        settings.Should().BeEquivalentTo(new InMemoryKafkaOffsetStoreSettings());
    }

    [Fact]
    public void WithName_ShouldSetOffsetStoreName()
    {
        InMemoryKafkaOffsetStoreSettingsBuilder builder = new();

        KafkaOffsetStoreSettings settings = builder.WithName("test-offsetStore").Build();

        settings.As<InMemoryKafkaOffsetStoreSettings>().OffsetStoreName.Should().Be("test-offsetStore");
    }
}
