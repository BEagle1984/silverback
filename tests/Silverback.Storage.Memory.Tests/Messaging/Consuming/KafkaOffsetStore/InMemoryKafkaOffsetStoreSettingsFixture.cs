// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Consuming.KafkaOffsetStore;

public class InMemoryKafkaOffsetStoreSettingsFixture
{
    [Fact]
    public void Constructor_ShouldCreateDefaultSettings()
    {
        InMemoryKafkaOffsetStoreSettings settings = new();

        settings.OffsetStoreName.ShouldBe("default");
    }

    [Fact]
    public void Constructor_ShouldSetOffsetStoreName()
    {
        InMemoryKafkaOffsetStoreSettings settings = new("my-offsets");

        settings.OffsetStoreName.ShouldBe("my-offsets");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        InMemoryKafkaOffsetStoreSettings kafkaOffsetStoreSettings = new("my-offsetStore");

        Action act = kafkaOffsetStoreSettings.Validate;

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenStoreNameIsEmptyOrWhitespace(string? offsetStoreName)
    {
        InMemoryKafkaOffsetStoreSettings kafkaOffsetStoreSettings = new(offsetStoreName!);

        Action act = kafkaOffsetStoreSettings.Validate;

        act.ShouldThrow<SilverbackConfigurationException>();
    }
}
