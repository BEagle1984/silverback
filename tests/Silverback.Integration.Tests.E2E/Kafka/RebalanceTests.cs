// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class RebalanceTests : KafkaTests
{
    public RebalanceTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Rebalance_ShouldConsumeAgainAfterRebalance_WhenUsingDefaultAssignmentStrategy()
    {
        PartitionCallbacksHandler partitionCallbacksHandler = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5))
                .ManuallyConnect())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddSingletonBrokerClientCallback(partitionCallbacksHandler)
            .AddIntegrationSpyAndSubscriber());

        KafkaConsumer[] consumers = [.. Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>()];
        await consumers[0].Client.ConnectAsync();

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(5);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);

        consumers[0].Client.Assignment.Count.ShouldBe(5);
        partitionCallbacksHandler.CurrentPartitions[consumers[0]].Count.ShouldBe(5);
        consumers[1].Client.Assignment.Count.ShouldBe(0);
        partitionCallbacksHandler.RevokedPartitions.ShouldNotContainKey(consumers[1]);
        partitionCallbacksHandler.CurrentPartitions.ShouldNotContainKey(consumers[1]);

        await consumers[1].Client.ConnectAsync();

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(10);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);

        consumers[0].Client.Assignment.Count.ShouldBe(3);
        partitionCallbacksHandler.RevokedPartitions[consumers[0]].Count.ShouldBe(5);
        partitionCallbacksHandler.CurrentPartitions[consumers[0]].Count.ShouldBe(3);
        consumers[1].Client.Assignment.Count.ShouldBe(2);
        partitionCallbacksHandler.RevokedPartitions.ShouldNotContainKey(consumers[1]);
        partitionCallbacksHandler.CurrentPartitions[consumers[1]].Count.ShouldBe(2);
        consumers.ShouldAllBe(consumer => consumer.StatusInfo.Status >= ConsumerStatus.Connected);
    }

    [Fact]
    public async Task Rebalance_ShouldConsumeAgainAfterRebalance_WhenUsingCooperativeAssignmentStrategy()
    {
        PartitionCallbacksHandler partitionCallbacksHandler = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5))
                .ManuallyConnect())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .WithCooperativeStickyPartitionAssignmentStrategy()
                    .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .WithCooperativeStickyPartitionAssignmentStrategy()
                    .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddSingletonBrokerClientCallback(partitionCallbacksHandler)
            .AddIntegrationSpyAndSubscriber());

        KafkaConsumer[] consumers = [.. Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>()];
        await consumers[0].Client.ConnectAsync();

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(5);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);

        consumers[0].Client.Assignment.Count.ShouldBe(5);
        partitionCallbacksHandler.CurrentPartitions[consumers[0]].Count.ShouldBe(5);
        consumers[1].Client.Assignment.Count.ShouldBe(0);
        partitionCallbacksHandler.RevokedPartitions.ShouldNotContainKey(consumers[1]);
        partitionCallbacksHandler.CurrentPartitions.ShouldNotContainKey(consumers[1]);

        await consumers[1].Client.ConnectAsync();

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(10);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);

        consumers[0].Client.Assignment.Count.ShouldBe(3);
        partitionCallbacksHandler.RevokedPartitions[consumers[0]].Count.ShouldBe(2);
        partitionCallbacksHandler.CurrentPartitions[consumers[0]].Count.ShouldBe(3);
        consumers[1].Client.Assignment.Count.ShouldBe(2);
        partitionCallbacksHandler.RevokedPartitions.ShouldNotContainKey(consumers[1]);
        partitionCallbacksHandler.CurrentPartitions[consumers[1]].Count.ShouldBe(2);
        consumers.ShouldAllBe(consumer => consumer.StatusInfo.Status >= ConsumerStatus.Connected);
    }

    [Fact]
    public async Task Rebalance_ShouldCommitPendingOffsets()
    {
        int receivedMessages = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5))
                .ManuallyConnect())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(10)
                    .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(10)
                    .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<TestEventWithKafkaKey>(HandleMessage));

        void HandleMessage(TestEventWithKafkaKey dummy) => Interlocked.Increment(ref receivedMessages);

        KafkaConsumer[] consumers = [.. Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>()];
        await consumers[0].Client.ConnectAsync();

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages == 5);

        receivedMessages.ShouldBe(5);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        await consumers[1].Client.ConnectAsync();

        await AsyncTestingUtil.WaitAsync(() => DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) == 5);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);
    }

    [Fact]
    public async Task Rebalance_ShouldAbortPendingBatchesAndConsumeAgainAfterRebalance()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5))
                .ManuallyConnect())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).EnableBatchProcessing(10)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).EnableBatchProcessing(10))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> eventsStream)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in eventsStream)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        KafkaConsumer[] consumers = [.. Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>()];
        await consumers[0].Client.ConnectAsync();

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        // Produce 1 1/2 batches per partition
        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 0)]);
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 1)]);
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 2)]);
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 3)]);
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 4)]);
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15 * 5);

        receivedBatches.Count.ShouldBe(10);
        completedBatches.ShouldBe(5);
        receivedBatches.Sum(batch => batch.Count).ShouldBe(15 * 5);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10 * 5);

        // Connect the second consumer to trigger the rebalance
        await consumers[1].Client.ConnectAsync();

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 20 * 5);

        receivedBatches.Count.ShouldBe(15);
        completedBatches.ShouldBe(5);
        receivedBatches.Sum(batch => batch.Count).ShouldBe(20 * 5);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10 * 5);

        // Produce another 1/2 batche per partition
        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 0)]);
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 1)]);
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 2)]);
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 3)]);
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 4)]);
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(15);
        completedBatches.ShouldBe(10);
        receivedBatches.Sum(batch => batch.Count).ShouldBe(25 * 5);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(20 * 5);
    }

    [Fact]
    public async Task Rebalance_ShouldAbortPendingBatchesAndConsumeAgainAfterRebalance_WhenUsingCooperativeAssignmentStrategy()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5))
                .ManuallyConnect())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .WithCooperativeStickyPartitionAssignmentStrategy()
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).EnableBatchProcessing(10)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .WithCooperativeStickyPartitionAssignmentStrategy()
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).EnableBatchProcessing(10))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> eventsStream)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in eventsStream)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        KafkaConsumer[] consumers = [.. Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>()];
        await consumers[0].Client.ConnectAsync();

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        // Produce 1 1/2 batches per partition
        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 0)]);
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 1)]);
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 2)]);
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 3)]);
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 4)]);
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15 * 5);

        receivedBatches.Count.ShouldBe(10);
        completedBatches.ShouldBe(5);
        receivedBatches.Sum(batch => batch.Count).ShouldBe(15 * 5);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10 * 5);

        // Connect the second consumer to trigger the rebalance
        await consumers[1].Client.ConnectAsync();

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == (15 * 5) + (5 * 2));

        receivedBatches.Count.ShouldBe(12); // cooperative assignment -> only half of the partitions are reassigned
        completedBatches.ShouldBe(5);
        receivedBatches.Sum(batch => batch.Count).ShouldBe((15 * 5) + (5 * 2));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10 * 5);

        // Produce another 1/2 batche per reassigned partition
        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 0)]);
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 1)]);
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 2)]);
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 3)]);
            await producer.ProduceAsync(new TestEventOne(), [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 4)]);
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(12);
        completedBatches.ShouldBe(10);
        receivedBatches.Sum(batch => batch.Count).ShouldBe((20 * 5) + (5 * 2));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(20 * 5);
    }

    [Fact]
    public async Task Rebalance_ShouldAbortChunkSequenceAndNotCommit_WhenRebalancingWithIncompleteJson()
    {
        TestEventOne message = new() { ContentEventOne = "Message 1" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(2))
                .ManuallyConnect())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        KafkaConsumer[] consumers = [.. Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>()];
        await consumers[0].Client.ConnectAsync();

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            [.. rawMessage.Take(5)],
            [
                .. HeadersHelper.GetChunkHeaders("1", 0, typeof(TestEventOne)),
                new MessageHeader(KafkaMessageHeaders.DestinationPartition, 1)
            ]);
        await producer.RawProduceAsync(
            [.. rawMessage.Skip(5).Take(5)],
            [
                .. HeadersHelper.GetChunkHeaders("1", 1, typeof(TestEventOne)),
                new MessageHeader(KafkaMessageHeaders.DestinationPartition, 1)
            ]);
        await producer.RawProduceAsync(
            [.. rawMessage.Skip(10).Take(5)],
            [
                .. HeadersHelper.GetChunkHeaders("1", 2, typeof(TestEventOne)),
                new MessageHeader(KafkaMessageHeaders.DestinationPartition, 1)
            ]);

        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.RawInboundEnvelopes.Count >= 3);
        Helper.Spy.RawInboundEnvelopes.Count.ShouldBe(3);

        await consumers[1].Client.ConnectAsync(); // This triggers a rebalance
        await AsyncTestingUtil.WaitAsync(() => consumers[1].StatusInfo.Status >= ConsumerStatus.Connected);

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(0);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        await producer.RawProduceAsync(
            [.. rawMessage.Skip(15)],
            [
                .. HeadersHelper.GetChunkHeaders("1", 3, true, typeof(TestEventOne)),
                new MessageHeader(KafkaMessageHeaders.DestinationPartition, 1)
            ]);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Count.ShouldBe(7);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(4);
    }

    [Fact]
    public async Task Rebalance_ShouldAbortChunkSequenceAndNotCommit_WhenRebalancingWithIncompleteBinaryMessage()
    {
        byte[] rawMessage = BytesUtil.GetRandomBytes();
        int aborted = 0;

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
                    .Consume<BinaryMessage>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<BinaryMessage>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
            .AddIntegrationSpy());

        async ValueTask HandleMessage(BinaryMessage binaryMessage)
        {
            try
            {
                await binaryMessage.Content.ReadAllAsync();
            }
            catch (OperationCanceledException)
            {
                aborted++;
                throw;
            }
        }

        KafkaConsumer[] consumers = [.. Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>()];
        await consumers[0].Client.ConnectAsync();

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            [.. rawMessage.Take(5)],
            HeadersHelper.GetChunkHeaders("1", 0, 3));
        await producer.RawProduceAsync(
            [.. rawMessage.Skip(5).Take(5)],
            HeadersHelper.GetChunkHeaders("1", 1, 3));

        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.RawInboundEnvelopes.Count >= 2);
        Helper.Spy.RawInboundEnvelopes.Count.ShouldBe(2);

        await consumers[1].Client.ConnectAsync(); // This triggers a rebalance
        await AsyncTestingUtil.WaitAsync(() => aborted == 1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        aborted.ShouldBe(1);

        await producer.RawProduceAsync(
            [.. rawMessage.Skip(10)],
            HeadersHelper.GetChunkHeaders("1", 2, 3));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Count.ShouldBe(5);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);
        aborted.ShouldBe(1);
    }

    private sealed class PartitionCallbacksHandler : IKafkaPartitionsAssignedCallback, IKafkaPartitionsRevokedCallback
    {
        public ConcurrentDictionary<IKafkaConsumer, List<TopicPartition>> CurrentPartitions { get; } = new();

        public ConcurrentDictionary<IKafkaConsumer, List<TopicPartition>> RevokedPartitions { get; } = new();

        public IEnumerable<TopicPartitionOffset>? OnPartitionsAssigned(
            IReadOnlyCollection<TopicPartition> topicPartitions,
            IKafkaConsumer consumer)
        {
            List<TopicPartition> consumerPartitions = CurrentPartitions.GetOrAdd(
                consumer,
                _ => []);

            consumerPartitions.AddRange(topicPartitions);

            return null;
        }

        public void OnPartitionsRevoked(IReadOnlyCollection<TopicPartitionOffset> topicPartitionsOffset, IKafkaConsumer consumer)
        {
            CurrentPartitions.TryGetValue(consumer, out List<TopicPartition>? consumerPartitions);

            List<TopicPartition> revokedPartitions = RevokedPartitions.GetOrAdd(
                consumer,
                _ => []);

            foreach (TopicPartitionOffset topicPartitionOffset in topicPartitionsOffset)
            {
                revokedPartitions.Add(topicPartitionOffset.TopicPartition);
                consumerPartitions?.Remove(topicPartitionOffset.TopicPartition);
            }
        }
    }
}
