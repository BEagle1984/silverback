// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Collections;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class EndpointTests
{
    [Fact]
    public void DisplayName_WithoutFriendlyNameSet_TopicNameReturned()
    {
        TestConsumerConfiguration configuration = new("name");

        configuration.DisplayName.Should().Be("name");
    }

    [Fact]
    public void DisplayName_WithFriendlyNameSetBeforeRawName_FriendlyAndTopicNameReturned()
    {
        TestConsumerConfiguration configuration = new()
        {
            FriendlyName = "display-name",
            TopicNames = new ValueReadOnlyCollection<string>(new[] { "name" })
        };

        configuration.DisplayName.Should().Be("display-name (name)");
    }

    [Fact]
    public void DisplayName_WithFriendlyNameSetAfterRawName_FriendlyAndTopicNameReturned()
    {
        TestConsumerConfiguration configuration = new()
        {
            TopicNames = new ValueReadOnlyCollection<string>(new[] { "name" }),
            FriendlyName = "display-name"
        };

        configuration.DisplayName.Should().Be("display-name (name)");
    }

    [Fact]
    public void Equals_DifferentFriendlyName_FalseReturned()
    {
        TestProducerConfiguration endpoint1 = new()
        {
            FriendlyName = "friendly-1"
        };
        TestProducerConfiguration endpoint2 = new()
        {
            FriendlyName = "friendly-2"
        };

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeFalse();
    }
}
