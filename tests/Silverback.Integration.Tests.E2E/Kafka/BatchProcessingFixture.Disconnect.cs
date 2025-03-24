// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class BatchProcessingFixture
{
    [Fact]
    public async Task Batch_ShouldAbortEnumeration_WhenDisconnecting()
    {
        int batchesCount = 0;
        int abortedCount = 0;
        TestingCollection<TestEventOne> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume<TestEventOne>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(10))))
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            Interlocked.Increment(ref batchesCount);

            try
            {
                await foreach (TestEventOne message in batch)
                {
                    receivedMessages.Add(message);
                }
            }
            catch (OperationCanceledException)
            {
                // Simulate something going on in the subscribed method
                await Task.Delay(300);

                Interlocked.Increment(ref abortedCount);
            }
        }

        IProducer producer = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName)));

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count == 3);
        await Helper.GetConsumerForEndpoint(DefaultTopicName).Client.DisconnectAsync();
        await AsyncTestingUtil.WaitAsync(() => abortedCount == batchesCount);

        receivedMessages.Count.ShouldBe(3);
        batchesCount.ShouldBe(1);
        abortedCount.ShouldBe(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldAbortEnumerationOfMultipleBatches_WhenDisconnecting()
    {
        int batchesCount = 0;
        int abortedCount = 0;
        TestingCollection<TestEventOne> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume<TestEventOne>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(10))))
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            Interlocked.Increment(ref batchesCount);

            try
            {
                await foreach (TestEventOne message in batch)
                {
                    receivedMessages.Add(message);
                }
            }
            catch (OperationCanceledException)
            {
                // Simulate something going on in the subscribed method
                await Task.Delay(300);

                Interlocked.Increment(ref abortedCount);
            }
        }

        IProducer producer = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName)));

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(
                new TestEventOne { ContentEventOne = $"{i}" },
                [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 0)]);
            await producer.ProduceAsync(
                new TestEventOne { ContentEventOne = $"{i}" },
                [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 1)]);
            await producer.ProduceAsync(
                new TestEventOne { ContentEventOne = $"{i}" },
                [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 2)]);
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count == 9);
        await Helper.GetConsumerForEndpoint(DefaultTopicName).Client.DisconnectAsync();
        await AsyncTestingUtil.WaitAsync(() => abortedCount == batchesCount);

        receivedMessages.Count.ShouldBe(9);
        batchesCount.ShouldBe(3);
        abortedCount.ShouldBe(3);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldAbortMaterialization_WhenDisconnecting()
    {
        int batchesCount = 0;
        int abortedCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume<TestEventOne>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(10))))
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleBatch));

        async Task HandleBatch(IMessageStreamEnumerable<TestEventOne> batch)
        {
            Interlocked.Increment(ref batchesCount);

            try
            {
                List<TestEventOne> dummy = [.. batch]; // Enumerate
            }
            catch (OperationCanceledException)
            {
                // Simulate something going on in the subscribed method
                await Task.Delay(300);

                Interlocked.Increment(ref abortedCount);
            }
        }

        IProducer producer = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName)));

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Task.Delay(100);

        await Helper.GetConsumerForEndpoint(DefaultTopicName).Client.DisconnectAsync();
        await AsyncTestingUtil.WaitAsync(() => abortedCount == batchesCount);

        batchesCount.ShouldBe(1);
        abortedCount.ShouldBe(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldAwaitSubscriberBeforeDisconnecting()
    {
        TestingCollection<TestEventOne> receivedMessages = [];
        bool hasSubscriberReturned = false;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume<TestEventOne>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(10))))
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleBatch));

        async Task HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne message in batch)
            {
                receivedMessages.Add(message);
            }

            await Task.Delay(500);

            hasSubscriberReturned = true;
        }

        IProducer producer = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(
                    endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .SetKafkaKey(envelope => envelope.Message?.ContentEventOne)));

        for (int i = 1; i <= 10; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 10);

        receivedMessages.Count.ShouldBe(10);
        hasSubscriberReturned.ShouldBeFalse();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        await Helper.GetConsumerForEndpoint(DefaultTopicName).Client.DisconnectAsync();

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);
        hasSubscriberReturned.ShouldBeTrue();
    }
}
