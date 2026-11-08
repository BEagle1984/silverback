// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Tests.Extended.Integration.TestHost.Kafka;
using Silverback.Tests.Extended.Shared.Kafka;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.Kafka;

[Collection(KafkaCollection.Name)]
[Trait("Type", "Integration")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "Kafka")]
public class TopicPatternSubscriptionTests(KafkaFixture fixture, ITestOutputHelper output)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan DiscoveryTimeout = TimeSpan.FromSeconds(60);

    private static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(3);

    [Theory]
    [InlineData(GroupProtocol.Classic)]
    [InlineData(GroupProtocol.Consumer)]
    public async Task Consume_ShouldConsumeMatchingTopicsOnly_WhenSubscribingPattern(GroupProtocol groupProtocol)
    {
        _ = fixture;

        string prefix = "stress-pattern-" + Guid.NewGuid().ToString("N");
        string pattern = CreatePattern(prefix);
        string topicA = prefix + ".a";
        string topicB = prefix + ".b";

        // These topics would match if the dot wasn't escaped or the pattern wasn't anchored at the end
        string unescapedDotTopic = prefix + "-a";
        string unanchoredEndTopic = prefix + ".a1";

        using IAdminClient admin = CreateAdminClient();
        await CreateTopicsAsync(admin, topicA, topicB, unescapedDotTopic, unanchoredEndTopic);

        using IProducer<Null, byte[]> producer = CreateProducer();
        await ProduceJsonAsync(producer, topicA, new ReconciliationMessage(0, 1));
        await ProduceJsonAsync(producer, topicB, new ReconciliationMessage(0, 2));
        await ProduceJsonAsync(producer, unescapedDotTopic, new ReconciliationMessage(0, 3));
        await ProduceJsonAsync(producer, unanchoredEndTopic, new ReconciliationMessage(0, 4));

        ConcurrentQueue<ConsumedMessage> consumed = new();
        AssignmentObserver assignments = new();
        using CollectingLoggerProvider logs = new();
        using IHost host = CreateHost(prefix, groupProtocol, consumed, assignments, logs, consumer => AddEndpoint(consumer, pattern));

        try
        {
            await host.StartAsync().WaitAsync(Timeout);
            await WaitUntilAsync(() => consumed.Distinct().Count() >= 2, Timeout, "the messages of the matching topics");

            // Leave time to consume the messages of the other topics, in case they were assigned as well
            await Task.Delay(SettleTime);

            assignments.Topics.ShouldBe([topicA, topicB], ignoreOrder: true);

            ConsumedMessage[] expected =
            [
                new(topicA, topicA, pattern, new ReconciliationMessage(0, 1)),
                new(topicB, topicB, pattern, new ReconciliationMessage(0, 2))
            ];
            consumed.Distinct().ShouldBe(expected, ignoreOrder: true);
            logs.Errors.ShouldBeEmpty();
        }
        finally
        {
            await StopHostAsync(host, assignments, consumed, logs);
        }
    }

    [Theory]
    [InlineData(GroupProtocol.Classic)]
    [InlineData(GroupProtocol.Consumer)]
    public async Task Consume_ShouldConsumeTopicCreatedLater_WhenSubscribingPattern(GroupProtocol groupProtocol)
    {
        _ = fixture;

        string prefix = "stress-pattern-" + Guid.NewGuid().ToString("N");
        string pattern = CreatePattern(prefix);
        string topicA = prefix + ".a";
        string topicB = prefix + ".b";

        using IAdminClient admin = CreateAdminClient();
        await CreateTopicsAsync(admin, topicA);

        using IProducer<Null, byte[]> producer = CreateProducer();
        await ProduceJsonAsync(producer, topicA, new ReconciliationMessage(0, 1));

        ConcurrentQueue<ConsumedMessage> consumed = new();
        AssignmentObserver assignments = new();
        using CollectingLoggerProvider logs = new();
        using IHost host = CreateHost(prefix, groupProtocol, consumed, assignments, logs, consumer => AddEndpoint(consumer, pattern));

        try
        {
            await host.StartAsync().WaitAsync(Timeout);
            await WaitUntilAsync(() => !consumed.IsEmpty, Timeout, "the message of the existing topic");

            await CreateTopicsAsync(admin, topicB);
            await ProduceJsonAsync(producer, topicB, new ReconciliationMessage(0, 2));

            // With the classic protocol, librdkafka matches the new topic at the next metadata refresh and rejoins the group to have
            // it assigned. With the consumer protocol, the broker resolves the pattern again when it learns about the new topic (at
            // most every 10 seconds, on a heartbeat) and assigns it incrementally.
            await WaitUntilAsync(
                () => consumed.Any(message => message.Topic == topicB),
                DiscoveryTimeout,
                "the message of the new topic");

            assignments.Topics.ShouldBe([topicA, topicB], ignoreOrder: true);

            ConsumedMessage[] expected =
            [
                new(topicA, topicA, pattern, new ReconciliationMessage(0, 1)),
                new(topicB, topicB, pattern, new ReconciliationMessage(0, 2))
            ];
            consumed.Distinct().ShouldBe(expected, ignoreOrder: true);
            logs.Errors.ShouldBeEmpty();
        }
        finally
        {
            await StopHostAsync(host, assignments, consumed, logs);
        }
    }

    [Theory]
    [InlineData(GroupProtocol.Classic)]
    [InlineData(GroupProtocol.Consumer)]
    public async Task Consume_ShouldPreferTopicNameEndpoint_WhenTopicAlsoMatchesPattern(GroupProtocol groupProtocol)
    {
        _ = fixture;

        string prefix = "stress-pattern-" + Guid.NewGuid().ToString("N");
        string pattern = CreatePattern(prefix);
        string topicA = prefix + ".a";
        string topicB = prefix + ".b";

        using IAdminClient admin = CreateAdminClient();
        await CreateTopicsAsync(admin, topicA, topicB);

        using IProducer<Null, byte[]> producer = CreateProducer();
        await ProduceJsonAsync(producer, topicA, new ReconciliationMessage(0, 1));
        await ProduceJsonAsync(producer, topicB, new ReconciliationMessage(0, 2));

        ConcurrentQueue<ConsumedMessage> consumed = new();
        AssignmentObserver assignments = new();
        using CollectingLoggerProvider logs = new();

        // The pattern endpoint is configured first, but the endpoint subscribing the topic name takes precedence
        using IHost host = CreateHost(
            prefix,
            groupProtocol,
            consumed,
            assignments,
            logs,
            consumer =>
            {
                AddEndpoint(consumer, pattern);
                AddEndpoint(consumer, topicB);
            });

        try
        {
            await host.StartAsync().WaitAsync(Timeout);
            await WaitUntilAsync(() => consumed.Distinct().Count() >= 2, Timeout, "the messages of both topics");

            assignments.Topics.ShouldBe([topicA, topicB], ignoreOrder: true);

            ConsumedMessage[] expected =
            [
                new(topicA, topicA, pattern, new ReconciliationMessage(0, 1)),
                new(topicB, topicB, topicB, new ReconciliationMessage(0, 2))
            ];
            consumed.Distinct().ShouldBe(expected, ignoreOrder: true);
            logs.Errors.ShouldBeEmpty();
        }
        finally
        {
            await StopHostAsync(host, assignments, consumed, logs);
        }
    }

    [Theory]
    [InlineData(GroupProtocol.Classic)]
    [InlineData(GroupProtocol.Consumer)]
    public async Task Consume_ShouldApplyMatchSemanticsOfGroupProtocol_WhenPatternIsNotAnchoredAtEnd(GroupProtocol groupProtocol)
    {
        _ = fixture;

        string prefix = "stress-pattern-" + Guid.NewGuid().ToString("N");
        string topicA = prefix + ".a";
        string longerTopic = prefix + ".a1";

        // Without the trailing '$', librdkafka (classic protocol) also subscribes the topic starting with the matched text, while
        // the broker (consumer protocol) requires the pattern to match the entire topic name
        string pattern = $"^{Regex.Escape(topicA)}";

        using IAdminClient admin = CreateAdminClient();
        await CreateTopicsAsync(admin, topicA, longerTopic);

        using IProducer<Null, byte[]> producer = CreateProducer();
        await ProduceJsonAsync(producer, topicA, new ReconciliationMessage(0, 1));
        await ProduceJsonAsync(producer, longerTopic, new ReconciliationMessage(0, 2));

        ConcurrentQueue<ConsumedMessage> consumed = new();
        AssignmentObserver assignments = new();
        using CollectingLoggerProvider logs = new();
        using IHost host = CreateHost(prefix, groupProtocol, consumed, assignments, logs, consumer => AddEndpoint(consumer, pattern));

        try
        {
            List<ConsumedMessage> expected = [new(topicA, topicA, pattern, new ReconciliationMessage(0, 1))];

            if (groupProtocol == GroupProtocol.Classic)
                expected.Add(new ConsumedMessage(longerTopic, longerTopic, pattern, new ReconciliationMessage(0, 2)));

            await host.StartAsync().WaitAsync(Timeout);
            await WaitUntilAsync(() => consumed.Distinct().Count() >= expected.Count, Timeout, "the messages of the matching topics");

            // Leave time to consume the messages of the other topic, in case it was assigned as well
            await Task.Delay(SettleTime);

            assignments.Topics.ShouldBe(expected.Select(message => message.Topic), ignoreOrder: true);
            consumed.Distinct().ShouldBe(expected, ignoreOrder: true);
            logs.Errors.ShouldBeEmpty();
        }
        finally
        {
            await StopHostAsync(host, assignments, consumed, logs);
        }
    }

    // Escaped and anchored, using only syntax that the librdkafka regex engines and .NET interpret the same way
    private static string CreatePattern(string prefix) => $"^{Regex.Escape(prefix + ".")}[a-z]+$";

    private static IAdminClient CreateAdminClient() =>
        new AdminClientBuilder(new AdminClientConfig { BootstrapServers = KafkaFixture.BootstrapServers }).Build();

    private static Task CreateTopicsAsync(IAdminClient admin, params string[] topics) =>
        admin.CreateTopicsAsync(
            topics.Select(topic => new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 }));

    private static IProducer<Null, byte[]> CreateProducer() =>
        new ProducerBuilder<Null, byte[]>(
            new ProducerConfig
            {
                BootstrapServers = KafkaFixture.BootstrapServers,
                Acks = Acks.All
            }).Build();

    private static Task ProduceJsonAsync(IProducer<Null, byte[]> producer, string topic, ReconciliationMessage message) =>
        producer.ProduceAsync(topic, new Message<Null, byte[]> { Value = JsonSerializer.SerializeToUtf8Bytes(message) });

    private static IHost CreateHost(
        string prefix,
        GroupProtocol groupProtocol,
        ConcurrentQueue<ConsumedMessage> consumed,
        AssignmentObserver assignments,
        ILoggerProvider logs,
        Action<KafkaConsumerConfigurationBuilder> configureEndpoints)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders().AddProvider(logs);
        builder.Services.AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddSingletonBrokerClientCallback(assignments)
            .AddDelegateSubscriber<IInboundEnvelope<ReconciliationMessage>>(
                envelope => consumed.Enqueue(ConsumedMessage.Create(envelope)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers(KafkaFixture.BootstrapServers)
                .AddConsumer(consumer =>
                {
                    // With the classic protocol, refresh the metadata every second to discover the new topics quickly (the default
                    // is 5 minutes), while with the consumer protocol the broker resolves the pattern. Consume the messages written
                    // to a topic before it was assigned.
                    consumer
                        .WithGroupId(prefix + "-group")
                        .WithClientId(prefix + "-consumer")
                        .WithGroupProtocol(groupProtocol)
                        .WithTopicMetadataRefreshIntervalMs(1000)
                        .AutoResetOffsetToEarliest();

                    configureEndpoints(consumer);
                }));

        return builder.Build();
    }

    private static void AddEndpoint(KafkaConsumerConfigurationBuilder consumer, string topic) =>
        consumer.Consume<ReconciliationMessage>(endpoint => endpoint
            .ConsumeFrom(topic)
            .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader()));

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout, string description)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (!condition())
        {
            stopwatch.Elapsed.ShouldBeLessThan(timeout, $"Timed out waiting for {description}");
            await Task.Delay(100);
        }
    }

    private async Task StopHostAsync(
        IHost host,
        AssignmentObserver assignments,
        ConcurrentQueue<ConsumedMessage> consumed,
        CollectingLoggerProvider logs)
    {
        try
        {
            await host.StopAsync().WaitAsync(Timeout);
        }
        finally
        {
            output.WriteLine($"librdkafka={Library.VersionString}");
            output.WriteLine($"Assigned partitions: {string.Join(", ", assignments.Partitions)}");
            output.WriteLine($"Consumed messages: {string.Join(", ", consumed)}");

            foreach (string entry in logs.Entries)
            {
                output.WriteLine($"Log: {entry}");
            }
        }
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

    private sealed class CollectingLoggerProvider : ILoggerProvider, ILogger
    {
        public ConcurrentQueue<string> Entries { get; } = new();

        public ConcurrentQueue<string> Errors { get; } = new();

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            string entry = $"{logLevel} {eventId.Id}: {formatter(state, exception)}";

            if (exception != null)
                entry += $" | {exception.GetType().Name}: {exception.Message}";

            Entries.Enqueue(entry);

            if (logLevel >= LogLevel.Error)
                Errors.Enqueue(entry);
        }

        public void Dispose()
        {
            // No resources to release
        }
    }
}
