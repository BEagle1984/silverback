// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class ParsedTopicTests
{
    [Fact]
    public void Topic_ShouldReturnTopicName_WhenNotSharedSubscription()
    {
        ParsedTopic parsedTopic = new("test");

        parsedTopic.Topic.ShouldBe("test");
    }

    [Fact]
    public void Topic_ShouldReturnTopicName_WhenSharedSubscription()
    {
        ParsedTopic parsedTopic = new("$share/group/test");

        parsedTopic.Topic.ShouldBe("test");
    }

    [Fact]
    public void Regex_ShouldBeNull_WhenTopicHasNoWildcards()
    {
        ParsedTopic parsedTopic = new("test");

        parsedTopic.Regex.ShouldBeNull();
    }

    [Theory]
    [InlineData("test/#")]
    [InlineData("test/+/sub")]
    [InlineData("#")]
    [InlineData("$share/group/test/+")]
    public void Regex_ShouldNotBeNull_WhenTopicHasWildcards(string topic)
    {
        ParsedTopic parsedTopic = new(topic);

        parsedTopic.Regex.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("test", "test", true)]
    [InlineData("test", "Test", false)]
    [InlineData("test", "test/sub", false)]
    [InlineData("test/+", "test/sub", true)]
    [InlineData("test/+", "test/", true)]
    [InlineData("test/+", "test", false)]
    [InlineData("test/+", "test/sub/sub", false)]
    [InlineData("test/+/sub", "test/1/sub", true)]
    [InlineData("test/+/sub", "test//sub", true)]
    [InlineData("test/+/sub", "test/1/2/sub", false)]
    [InlineData("+/sub", "/sub", true)]
    [InlineData("+", "test", true)]
    [InlineData("+", "test/sub", false)]
    [InlineData("test/#", "test", true)]
    [InlineData("test/#", "test/", true)]
    [InlineData("test/#", "test/sub", true)]
    [InlineData("test/#", "test/sub/sub", true)]
    [InlineData("test/#", "test1", false)]
    [InlineData("test/#", "tests/sub", false)]
    [InlineData("test/+/#", "test/sub", true)]
    [InlineData("test/+/#", "test/sub/sub/sub", true)]
    [InlineData("test/+/#", "test", false)]
    [InlineData("+/#", "test", true)]
    [InlineData("#", "test/sub", true)]
    [InlineData("#", "$SYS/uptime", false)]
    [InlineData("+/uptime", "$SYS/uptime", false)]
    [InlineData("$SYS/#", "$SYS/uptime", true)]
    [InlineData("$SYS/+", "$SYS/uptime", true)]
    [InlineData("test/$x/+", "test/$x/sub", true)]
    [InlineData("a.b/+", "a.b/sub", true)]
    [InlineData("a.b/+", "axb/sub", false)]
    [InlineData("a(b)/+", "a(b)/sub", true)]
    [InlineData("test/#", "test/line1\nline2", true)]
    [InlineData("test/+/sub", "test/1/sub\n", false)]
    [InlineData("$share/group/test/#", "test", true)]
    [InlineData("$share/group/test/#", "test/sub", true)]
    [InlineData("$share/group/test", "test", true)]
    [InlineData("$share/group/test", "$share/group/test", false)]
    public void IsMatch_ShouldMatchTopicAccordingToMqttFilterRules(string topicFilter, string topic, bool expected)
    {
        ParsedTopic parsedTopic = new(topicFilter);

        parsedTopic.IsMatch(topic).ShouldBe(expected);
    }

    [Theory]
    [InlineData("$share/group/test", "group", "test")]
    [InlineData("$share/group/test/+/#", "group", "test/+/#")]
    [InlineData("$share/group", "group", "")]
    [InlineData("$share//test", "", "test")]
    public void TrySplitSharedSubscription_ShouldReturnGroupAndTopic_WhenSharedSubscription(
        string topic,
        string expectedGroup,
        string expectedTopic)
    {
        bool result = ParsedTopic.TrySplitSharedSubscription(topic, out string? group, out string? actualTopic);

        result.ShouldBeTrue();
        group.ShouldBe(expectedGroup);
        actualTopic.ShouldBe(expectedTopic);
    }

    [Theory]
    [InlineData("test")]
    [InlineData("$SHARE/group/test")]
    [InlineData("$sharegroup/test")]
    public void TrySplitSharedSubscription_ShouldReturnFalse_WhenNotSharedSubscription(string topic)
    {
        bool result = ParsedTopic.TrySplitSharedSubscription(topic, out string? group, out string? actualTopic);

        result.ShouldBeFalse();
        group.ShouldBeNull();
        actualTopic.ShouldBeNull();
    }

    [Fact]
    public void SharedSubscriptionGroup_ShouldBeNull_WhenNotSharedSubscription()
    {
        ParsedTopic parsedTopic = new("test");

        parsedTopic.SharedSubscriptionGroup.ShouldBeNull();
    }

    [Fact]
    public void SharedSubscriptionGroup_ShouldReturnGroup_WhenSharedSubscription()
    {
        ParsedTopic parsedTopic = new("$share/group/test");

        parsedTopic.SharedSubscriptionGroup.ShouldBe("group");
    }
}
