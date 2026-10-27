// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

// Covers the static partition assignment specifics of the Dekaf integration: Dekaf doesn't auto-commit for a consumer
// that isn't a joined group member and the explicit start offsets must win over the committed offsets
public class StaticAssignmentTests : KafkaTests
{
    public StaticAssignmentTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // Use case: a statically assigned consumer relying on the default auto-commit must commit while running, not only
    // when it is closed, otherwise a crash would lose the whole progress
    [Fact]
    public async Task StaticAssignment_ShouldCommitPeriodically_WhenAutoCommitIsEnabled()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .WithAutoCommitIntervalMs(100)
                    .Consume(endpoint => endpoint.ConsumeFrom(new TopicPartitionOffset(DefaultTopicName, 0, Offset.Beginning)))))
            .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.InboundEnvelopes.Count == 5);
        await AsyncTestingUtil.WaitAsync(() => DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) == 5);

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(5);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);
    }

    // Use case: a consumer restarted with an explicit start offset replays the partition from that offset, regardless
    // of the offset previously committed by its group
    [Fact]
    public async Task StaticAssignment_ShouldStartFromSpecifiedOffset_WhenGroupHasCommittedOffset()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .ManuallyConnect())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableAutoCommit()
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(new TopicPartitionOffset(DefaultTopicName, 0, Offset.Beginning))))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableAutoCommit()
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(new TopicPartitionOffset(DefaultTopicName, 0, 5)))))
            .AddIntegrationSpyAndSubscriber());

        KafkaConsumer[] kafkaConsumers = [.. Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>()];
        KafkaConsumer fromBeginningConsumer = kafkaConsumers.Single(consumer => GetStartOffset(consumer) == Offset.Beginning);
        KafkaConsumer fromOffsetConsumer = kafkaConsumers.Single(consumer => GetStartOffset(consumer) == 5);
        IProducer producer = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName)));

        for (int i = 0; i < 10; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await fromBeginningConsumer.Client.ConnectAsync();
        await AsyncTestingUtil.WaitAsync(() => DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) == 10);
        await fromBeginningConsumer.Client.DisconnectAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(10);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);

        await fromOffsetConsumer.Client.ConnectAsync();
        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.InboundEnvelopes.Count == 15);

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(15);
        Helper.Spy.InboundEnvelopes.Skip(10)
            .Select(envelope => ((KafkaOffset)envelope.BrokerMessageIdentifier).Offset.Value)
            .ShouldBe([5, 6, 7, 8, 9]);
    }

    private static Offset GetStartOffset(KafkaConsumer consumer) =>
        consumer.Configuration.Endpoints.Single().TopicPartitions.Single().Offset;
}
