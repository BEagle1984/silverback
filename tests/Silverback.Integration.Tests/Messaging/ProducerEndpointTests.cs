// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging;

public class ProducerEndpointTests
{
    [Theory]
    [InlineData(null, "topic")]
    [InlineData("", "topic")]
    [InlineData("friendly", "friendly (topic)")]
    public void DisplayName_ProperlyComposed(string? friendlyName, string expectedDisplayName)
    {
        TestProducerEndpoint configuration = new(
            "topic",
            new TestProducerConfiguration
            {
                FriendlyName = friendlyName
            });

        configuration.DisplayName.Should().Be(expectedDisplayName);
    }

    [Fact]
    public void Equals_SameInstance_TrueReturned()
    {
        TestProducerEndpoint configuration1 = new("test", new TestProducerConfiguration());
        TestProducerEndpoint configuration2 = configuration1;

        bool result = configuration1.Equals(configuration2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameTopic_TrueReturned()
    {
        TestProducerEndpoint configuration1 = new("test", new TestProducerConfiguration());
        TestProducerEndpoint configuration2 = new("test", new TestProducerConfiguration());

        bool result = configuration1.Equals(configuration2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentTopic_FalseReturned()
    {
        TestProducerEndpoint configuration1 = new("test1", new TestProducerConfiguration());
        TestProducerEndpoint configuration2 = new("test2", new TestProducerConfiguration());

        bool result = configuration1.Equals(configuration2);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentEndpointConfiguration_FalseReturned()
    {
        TestProducerEndpoint configuration1 = new(
            "test",
            new TestProducerConfiguration
            {
                Strategy = new OutboxProduceStrategy(new InMemoryOutboxSettings())
            });
        TestProducerEndpoint configuration2 = new("test", new TestProducerConfiguration());

        bool result = configuration1.Equals(configuration2);

        result.Should().BeFalse();
    }
}
