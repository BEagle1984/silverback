// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Confluent.Kafka;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Tests.Extended.Integration.TestHost;
using Silverback.Tests.Extended.Integration.TestHost.Kafka;
using Silverback.Tests.Extended.Shared.Kafka;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.Kafka;

[Collection(KafkaCollection.Name)]
[Trait("Type", "Integration")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "Kafka")]
public class TopicPatternSubscriptionTests : BrokerHarnessTests<KafkaTestHarness>
{
    private static readonly TimeSpan DiscoveryTimeout = TimeSpan.FromSeconds(60);

    private static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(3);

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    public TopicPatternSubscriptionTests(KafkaFixture fixture, ITestOutputHelper output)
        : base(new KafkaTestHarness("pattern", output))
    {
    }

    [Theory]
    [InlineData(GroupProtocol.Classic)]
    [InlineData(GroupProtocol.Consumer)]
    public async Task Consume_ShouldConsumeMatchingTopicsOnly_WhenSubscribingPattern(GroupProtocol groupProtocol)
    {
        string pattern = CreatePattern(Harness.Prefix);
        string topicA = Harness.Prefix + ".a";
        string topicB = Harness.Prefix + ".b";

        // These topics would match if the dot wasn't escaped or the pattern wasn't anchored at the end
        string unescapedDotTopic = Harness.Prefix + "-a";
        string unanchoredEndTopic = Harness.Prefix + ".a1";

        await Harness.CreateTopicsAsync(topicA, topicB, unescapedDotTopic, unanchoredEndTopic);
        await Harness.ProduceJsonAsync(topicA, new ReconciliationMessage(0, 1));
        await Harness.ProduceJsonAsync(topicB, new ReconciliationMessage(0, 2));
        await Harness.ProduceJsonAsync(unescapedDotTopic, new ReconciliationMessage(0, 3));
        await Harness.ProduceJsonAsync(unanchoredEndTopic, new ReconciliationMessage(0, 4));

        ConcurrentQueue<ConsumedMessage> consumed = new();
        AssignmentObserver assignments = new();
        ConfigureConsumer(groupProtocol, consumed, assignments, consumer => AddEndpoint(consumer, pattern));

        await Harness.StartHostAsync();
        await Harness.WaitUntilAsync(() => consumed.Distinct().Count() >= 2, "the messages of the matching topics");

        // Leave time to consume the messages of the other topics, in case they were assigned as well
        await Task.Delay(SettleTime);

        assignments.Topics.ShouldBe([topicA, topicB], ignoreOrder: true);

        ConsumedMessage[] expected =
        [
            new(topicA, topicA, pattern, new ReconciliationMessage(0, 1)),
            new(topicB, topicB, pattern, new ReconciliationMessage(0, 2))
        ];
        consumed.Distinct().ShouldBe(expected, ignoreOrder: true);
        Harness.Logs.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(GroupProtocol.Classic)]
    [InlineData(GroupProtocol.Consumer)]
    public async Task Consume_ShouldConsumeTopicCreatedLater_WhenSubscribingPattern(GroupProtocol groupProtocol)
    {
        string pattern = CreatePattern(Harness.Prefix);
        string topicA = Harness.Prefix + ".a";
        string topicB = Harness.Prefix + ".b";

        await Harness.CreateTopicsAsync(topicA);
        await Harness.ProduceJsonAsync(topicA, new ReconciliationMessage(0, 1));

        ConcurrentQueue<ConsumedMessage> consumed = new();
        AssignmentObserver assignments = new();
        ConfigureConsumer(groupProtocol, consumed, assignments, consumer => AddEndpoint(consumer, pattern));

        await Harness.StartHostAsync();
        await Harness.WaitUntilAsync(() => !consumed.IsEmpty, "the message of the existing topic");

        await Harness.CreateTopicsAsync(topicB);
        await Harness.ProduceJsonAsync(topicB, new ReconciliationMessage(0, 2));

        // With the classic protocol, librdkafka matches the new topic at the next metadata refresh and rejoins the group to have
        // it assigned. With the consumer protocol, the broker resolves the pattern again when it learns about the new topic (at
        // most every 10 seconds, on a heartbeat) and assigns it incrementally.
        await Harness.WaitUntilAsync(
            () => consumed.Any(message => message.Topic == topicB),
            "the message of the new topic",
            DiscoveryTimeout);

        assignments.Topics.ShouldBe([topicA, topicB], ignoreOrder: true);

        ConsumedMessage[] expected =
        [
            new(topicA, topicA, pattern, new ReconciliationMessage(0, 1)),
            new(topicB, topicB, pattern, new ReconciliationMessage(0, 2))
        ];
        consumed.Distinct().ShouldBe(expected, ignoreOrder: true);
        Harness.Logs.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(GroupProtocol.Classic)]
    [InlineData(GroupProtocol.Consumer)]
    public async Task Consume_ShouldPreferTopicNameEndpoint_WhenTopicAlsoMatchesPattern(GroupProtocol groupProtocol)
    {
        string pattern = CreatePattern(Harness.Prefix);
        string topicA = Harness.Prefix + ".a";
        string topicB = Harness.Prefix + ".b";

        await Harness.CreateTopicsAsync(topicA, topicB);
        await Harness.ProduceJsonAsync(topicA, new ReconciliationMessage(0, 1));
        await Harness.ProduceJsonAsync(topicB, new ReconciliationMessage(0, 2));

        ConcurrentQueue<ConsumedMessage> consumed = new();
        AssignmentObserver assignments = new();

        // The pattern endpoint is configured first, but the endpoint subscribing the topic name takes precedence
        ConfigureConsumer(
            groupProtocol,
            consumed,
            assignments,
            consumer =>
            {
                AddEndpoint(consumer, pattern);
                AddEndpoint(consumer, topicB);
            });

        await Harness.StartHostAsync();
        await Harness.WaitUntilAsync(() => consumed.Distinct().Count() >= 2, "the messages of both topics");

        assignments.Topics.ShouldBe([topicA, topicB], ignoreOrder: true);

        ConsumedMessage[] expected =
        [
            new(topicA, topicA, pattern, new ReconciliationMessage(0, 1)),
            new(topicB, topicB, topicB, new ReconciliationMessage(0, 2))
        ];
        consumed.Distinct().ShouldBe(expected, ignoreOrder: true);
        Harness.Logs.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(GroupProtocol.Classic)]
    [InlineData(GroupProtocol.Consumer)]
    public async Task Consume_ShouldApplyMatchSemanticsOfGroupProtocol_WhenPatternIsNotAnchoredAtEnd(GroupProtocol groupProtocol)
    {
        string topicA = Harness.Prefix + ".a";
        string longerTopic = Harness.Prefix + ".a1";

        // Without the trailing '$', librdkafka (classic protocol) also subscribes the topic starting with the matched text, while
        // the broker (consumer protocol) requires the pattern to match the entire topic name
        string pattern = $"^{Regex.Escape(topicA)}";

        await Harness.CreateTopicsAsync(topicA, longerTopic);
        await Harness.ProduceJsonAsync(topicA, new ReconciliationMessage(0, 1));
        await Harness.ProduceJsonAsync(longerTopic, new ReconciliationMessage(0, 2));

        ConcurrentQueue<ConsumedMessage> consumed = new();
        AssignmentObserver assignments = new();
        ConfigureConsumer(groupProtocol, consumed, assignments, consumer => AddEndpoint(consumer, pattern));

        List<ConsumedMessage> expected = [new(topicA, topicA, pattern, new ReconciliationMessage(0, 1))];

        if (groupProtocol == GroupProtocol.Classic)
            expected.Add(new ConsumedMessage(longerTopic, longerTopic, pattern, new ReconciliationMessage(0, 2)));

        await Harness.StartHostAsync();
        await Harness.WaitUntilAsync(() => consumed.Distinct().Count() >= expected.Count, "the messages of the matching topics");

        // Leave time to consume the messages of the other topic, in case it was assigned as well
        await Task.Delay(SettleTime);

        assignments.Topics.ShouldBe(expected.Select(message => message.Topic), ignoreOrder: true);
        consumed.Distinct().ShouldBe(expected, ignoreOrder: true);
        Harness.Logs.Errors.ShouldBeEmpty();
    }

    // Escaped and anchored, using only syntax that the librdkafka regex engines and .NET interpret the same way
    private static string CreatePattern(string prefix) => $"^{Regex.Escape(prefix + ".")}[a-z]+$";

    private static void AddEndpoint(KafkaConsumerConfigurationBuilder consumer, string topic) =>
        consumer.Consume<ReconciliationMessage>(endpoint => endpoint
            .ConsumeFrom(topic)
            .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader()));

    private void ConfigureConsumer(
        GroupProtocol groupProtocol,
        ConcurrentQueue<ConsumedMessage> consumed,
        AssignmentObserver assignments,
        Action<KafkaConsumerConfigurationBuilder> configureEndpoints)
    {
        Harness.ConfigureSilverback(silverback => silverback
            .AddSingletonBrokerClientCallback(assignments)
            .AddDelegateSubscriber<IInboundEnvelope<ReconciliationMessage>>(envelope => consumed.Enqueue(ConsumedMessage.Create(envelope))));
        Harness.AddClients(clients => clients
            .AddConsumer(consumer =>
            {
                // With the classic protocol, refresh the metadata every second to discover the new topics quickly (the default
                // is 5 minutes), while with the consumer protocol the broker resolves the pattern. Consume the messages written
                // to a topic before it was assigned.
                consumer
                    .WithGroupId(Harness.Prefix + "-group")
                    .WithClientId(Harness.Prefix + "-consumer")
                    .WithGroupProtocol(groupProtocol)
                    .WithTopicMetadataRefreshIntervalMs(1000)
                    .AutoResetOffsetToEarliest();

                configureEndpoints(consumer);
            }));
        Harness.OnStopped(() =>
        {
            Harness.Output.WriteLine($"librdkafka={Library.VersionString}");
            Harness.Output.WriteLine($"Assigned partitions: {string.Join(", ", assignments.Partitions)}");
            Harness.Output.WriteLine($"Consumed messages: {string.Join(", ", consumed)}");

            return Task.CompletedTask;
        });
    }

    // The consumed topic, the topic of the envelope's endpoint and the topic or pattern subscribed by its configuration
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Used for testing via equality")]
    private sealed record ConsumedMessage(string Topic, string EndpointTopic, string Subscription, ReconciliationMessage? Message)
    {
        public static ConsumedMessage Create(IInboundEnvelope<ReconciliationMessage> envelope) =>
            new(
                ((KafkaOffset)envelope.BrokerMessageIdentifier).TopicPartition.Topic,
                envelope.Endpoint.RawName,
                envelope.Endpoint.Configuration.RawName,
                envelope.Message);
    }

    private sealed class AssignmentObserver : IKafkaPartitionsAssignedCallback
    {
        public ConcurrentQueue<TopicPartition> Partitions { get; } = new();

        public IEnumerable<string> Topics => Partitions.Select(partition => partition.Topic).Distinct();

        public IEnumerable<TopicPartitionOffset>? OnPartitionsAssigned(
            IReadOnlyCollection<TopicPartition> topicPartitions,
            IKafkaConsumer consumer)
        {
            foreach (TopicPartition topicPartition in topicPartitions)
            {
                Partitions.Enqueue(topicPartition);
            }

            // Keep the assignment, resuming from the committed offsets
            return null;
        }
    }
}
