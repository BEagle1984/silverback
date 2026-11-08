// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.RegularExpressions;
using Confluent.Kafka;
using Shouldly;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaTopicPatternTests
{
    [Theory]
    [InlineData("^topic", true)]
    [InlineData("^", true)]
    [InlineData("topic", false)]
    [InlineData("topic^", false)]
    public void IsPattern_ShouldReturnTrue_WhenTopicStartsWithCaret(string topic, bool expected) =>
        KafkaTopicPattern.IsPattern(topic).ShouldBe(expected);

    [Theory]
    [InlineData("topic", "topic", true)]
    [InlineData("topic", "topic1", false)]
    [InlineData("topic", "Topic", false)]
    [InlineData("^topic-[a-z]+$", "topic-abc", true)]
    [InlineData("^topic-[a-z]+$", "topic-abc1", false)]
    [InlineData("^topic", "topic-abc", true)]
    [InlineData("^topic", "my-topic", false)]
    [InlineData("^topic\\.v[0-9]+", "topic.v2", true)]
    [InlineData("^topic\\.v[0-9]+", "topic-v2", false)]
    public void IsMatch_ShouldMatchTopicNameOrPattern(string subscription, string topic, bool expected) =>
        KafkaTopicPattern.IsMatch(subscription, topic).ShouldBe(expected);

    [Theory]
    [InlineData("topic", "topic", true)]
    [InlineData("^topic", "topic", true)]
    [InlineData("^topic", "topic-abc", false)]
    [InlineData("^topic-.*", "topic-abc", true)]
    [InlineData("^topic-[a-z]+$", "topic-abc", true)]
    [InlineData("^topic-a|^topic-b", "topic-b", true)]
    [InlineData("^topic-a|^topic-b", "topic-bc", false)]
    [InlineData("^topic-a|topic-b", "topic-b", true)]
    [InlineData("^topic-a|topic-b", "my-topic-b", false)]
    public void IsMatch_ShouldRequireMatchingEntireName_WhenFullMatchIsRequired(string subscription, string topic, bool expected) =>
        KafkaTopicPattern.IsMatch(subscription, topic, true).ShouldBe(expected);

    [Theory]
    [InlineData(null, false)]
    [InlineData(GroupProtocol.Classic, false)]
    [InlineData(GroupProtocol.Consumer, true)]
    public void IsFullMatchRequired_ShouldReturnTrue_WhenUsingConsumerProtocol(GroupProtocol? groupProtocol, bool expected) =>
        KafkaTopicPattern.IsFullMatchRequired(groupProtocol).ShouldBe(expected);

    [Fact]
    public void Create_ShouldReturnRegexMatchingEntireName_WhenFullMatchIsRequired()
    {
        Regex regex = KafkaTopicPattern.Create("^topic", true);

        regex.IsMatch("topic").ShouldBeTrue();
        regex.IsMatch("topic-abc").ShouldBeFalse();
    }

    [Fact]
    public void Create_ShouldReturnMatchingRegex() =>
        KafkaTopicPattern.Create("^topic-[a-z]+$").IsMatch("topic-abc").ShouldBeTrue();

    [Fact]
    public void Create_ShouldThrow_WhenPatternIsInvalid()
    {
        Action act = () => KafkaTopicPattern.Create("^topic-[a-z");

        act.ShouldThrow<ArgumentException>();
    }
}
