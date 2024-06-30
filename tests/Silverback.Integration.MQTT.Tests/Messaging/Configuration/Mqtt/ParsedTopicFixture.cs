// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class ParsedTopicFixture
{
    [Fact]
    public void Topic_ShouldReturnTopicName_WhenNotSharedSubscription()
    {
        ParsedTopic parsedTopic = new("test");

        parsedTopic.Topic.Should().Be("test");
    }

    [Fact]
    public void Topic_ShouldReturnTopicName_WhenSharedSubscription()
    {
        ParsedTopic parsedTopic = new("$share/group/test");

        parsedTopic.Topic.Should().Be("test");
    }

    [Fact]
    public void Regex_ShouldBeNull_WhenTopicHasNoWildcards()
    {
        ParsedTopic parsedTopic = new("test");

        parsedTopic.Regex.Should().BeNull();
    }

    [Theory]
    [InlineData("test/#", "^test/.*$")]
    [InlineData("test/+/sub", "^test/[^\\/]*/sub$")]
    [InlineData("test/+/sub/#", "^test/[^\\/]*/sub/.*$")]
    public void Regex_ShouldBeCorrectlyGenerated(string topic, string regex)
    {
        ParsedTopic parsedTopic = new(topic);

        parsedTopic.Regex.ShouldNotBeNull();
        parsedTopic.Regex.ToString().Should().Be(regex);
    }

    [Fact]
    public void SharedSubscriptionGroup_ShouldBeNull_WhenNotSharedSubscription()
    {
        ParsedTopic parsedTopic = new("test");

        parsedTopic.SharedSubscriptionGroup.Should().BeNull();
    }

    [Fact]
    public void SharedSubscriptionGroup_ShouldReturnGroup_WhenSharedSubscription()
    {
        ParsedTopic parsedTopic = new("$share/group/test");

        parsedTopic.SharedSubscriptionGroup.Should().Be("group");
    }
}
