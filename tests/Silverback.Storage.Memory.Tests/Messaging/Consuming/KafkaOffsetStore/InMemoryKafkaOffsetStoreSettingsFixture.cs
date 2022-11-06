// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Consuming.KafkaOffsetStore;

public class InMemoryKafkaOffsetStoreSettingsFixture
{
    [Fact]
    public void Constructor_ShouldCreateDefaultSettings()
    {
        InMemoryKafkaOffsetStoreSettings settings = new();

        settings.OffsetStoreName.Should().Be("default");
    }

    [Fact]
    public void Constructor_ShouldSetOffsetStoreName()
    {
        InMemoryKafkaOffsetStoreSettings settings = new("my-offsets");

        settings.OffsetStoreName.Should().Be("my-offsets");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        InMemoryKafkaOffsetStoreSettings kafkaOffsetStoreSettings = new("my-offsetStore");

        Action act = () => kafkaOffsetStoreSettings.Validate();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenStoreNameIsNullOrWhitespace(string? offsetStoreName)
    {
        InMemoryKafkaOffsetStoreSettings kafkaOffsetStoreSettings = new(offsetStoreName!);

        Action act = () => kafkaOffsetStoreSettings.Validate();

        act.Should().Throw<SilverbackConfigurationException>();
    }
}
