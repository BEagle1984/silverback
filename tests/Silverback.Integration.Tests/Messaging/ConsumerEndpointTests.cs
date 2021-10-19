// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging;

public class ConsumerEndpointTests
{
    [Theory]
    [InlineData(null, "topic")]
    [InlineData("", "topic")]
    [InlineData("friendly", "friendly (topic)")]
    public void DisplayName_ProperlyComposed(string? friendlyName, string expectedDisplayName)
    {
        TestConsumerEndpoint configuration = new(
            "topic",
            new TestConsumerConfiguration
            {
                FriendlyName = friendlyName
            });

        configuration.DisplayName.Should().Be(expectedDisplayName);
    }

    [Fact]
    public void Equals_SameInstance_TrueReturned()
    {
        TestConsumerEndpoint configuration1 = new("test", new TestConsumerConfiguration());
        TestConsumerEndpoint configuration2 = configuration1;

        bool result = configuration1.Equals(configuration2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameTopic_TrueReturned()
    {
        TestConsumerEndpoint configuration1 = new("test", new TestConsumerConfiguration());
        TestConsumerEndpoint configuration2 = new("test", new TestConsumerConfiguration());

        bool result = configuration1.Equals(configuration2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentTopic_FalseReturned()
    {
        TestConsumerEndpoint configuration1 = new("test1", new TestConsumerConfiguration());
        TestConsumerEndpoint configuration2 = new("test2", new TestConsumerConfiguration());

        bool result = configuration1.Equals(configuration2);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentEndpointConfiguration_FalseReturned()
    {
        TestConsumerEndpoint configuration1 = new(
            "test",
            new TestConsumerConfiguration
            {
                GroupId = "123"
            });
        TestConsumerEndpoint configuration2 = new("test", new TestConsumerConfiguration());

        bool result = configuration1.Equals(configuration2);

        result.Should().BeFalse();
    }
}
