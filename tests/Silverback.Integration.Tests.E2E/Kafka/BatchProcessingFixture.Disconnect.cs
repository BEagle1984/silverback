// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
        TestingCollection<TestEventOne> receivedMessages = new();

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
                .Produce<TestEventOne>(
                    endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithKafkaKey(envelope => envelope.Message?.ContentEventOne)));

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count == 3);
        await Helper.GetConsumerForEndpoint(DefaultTopicName).Client.DisconnectAsync();
        await AsyncTestingUtil.WaitAsync(() => abortedCount == batchesCount);

        receivedMessages.Should().HaveCount(3);
        batchesCount.Should().Be(1);
        abortedCount.Should().Be(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
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
                List<TestEventOne> dummy = batch.ToList();
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
                .Produce<TestEventOne>(
                    endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithKafkaKey(envelope => envelope.Message?.ContentEventOne)));

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Task.Delay(100);

        await Helper.GetConsumerForEndpoint(DefaultTopicName).Client.DisconnectAsync();
        await AsyncTestingUtil.WaitAsync(() => abortedCount == batchesCount);

        batchesCount.Should().Be(1);
        abortedCount.Should().Be(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
    }

    [Fact]
    public async Task Batch_ShouldAwaitSubscriberBeforeDisconnecting()
    {
        TestingCollection<TestEventOne> receivedMessages = new();
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
                        .WithKafkaKey(envelope => envelope.Message?.ContentEventOne)));

        for (int i = 1; i <= 10; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 10);

        receivedMessages.Should().HaveCount(10);
        hasSubscriberReturned.Should().BeFalse();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);

        await Helper.GetConsumerForEndpoint(DefaultTopicName).Client.DisconnectAsync();

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);
        hasSubscriberReturned.Should().BeTrue();
    }
}
