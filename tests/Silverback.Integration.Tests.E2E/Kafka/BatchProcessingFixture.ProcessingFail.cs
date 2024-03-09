// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class BatchProcessingFixture
{
    [Fact]
    public async Task Batch_ShouldStopConsumer_WhenProcessingFails()
    {
        int receivedMessages = 0;

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
                                        .EnableBatchProcessing(3))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne dummy in batch)
            {
                Interlocked.Increment(ref receivedMessages);

                if (receivedMessages == 2)
                    throw new InvalidOperationException("Test");
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Message 1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Message 2" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Message 3" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Should().Be(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
        Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.Status.Should().Be(ConsumerStatus.Stopped);
    }

    [Fact]
    public async Task Batch_ShouldStopConsumer_WhenProcessingFailsWithMultiplePendingBatches()
    {
        int batchesCount = 0;
        int abortedCount = 0;

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
                                .Consume<TestEventWithKafkaKey>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(10))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventWithKafkaKey>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventWithKafkaKey> eventsStream)
        {
            int batchIndex = Interlocked.Increment(ref batchesCount);

            int messagesCount = 0;

            try
            {
                await foreach (TestEventWithKafkaKey dummy in eventsStream)
                {
                    if (batchIndex == 2 && ++messagesCount == 2)
                        throw new InvalidOperationException("Test");
                }
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(ref abortedCount);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 0; i < 10; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i, Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        batchesCount.Should().BeGreaterThan(1);
        abortedCount.Should().BeGreaterOrEqualTo(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
        Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.Status.Should().Be(ConsumerStatus.Stopped);
    }

    // TODO: Check flaky test (CI)
    [Fact]
    public async Task Batch_ShouldStopConsumer_WhenProcessingFailsWithIncompatibleSubscriber()
    {
        int batchesCount = 0;
        int abortedCount = 0;

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
                                .Consume<TestEventWithKafkaKey>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(10))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventWithKafkaKey>>(HandleBatch)
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventTwo>>(HandleIncompatibleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventWithKafkaKey> eventsStream)
        {
            int batchIndex = Interlocked.Increment(ref batchesCount);

            int messagesCount = 0;

            try
            {
                await foreach (TestEventWithKafkaKey dummy in eventsStream)
                {
                    if (batchIndex == 2 && ++messagesCount == 2)
                        throw new InvalidOperationException("Test");
                }
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(ref abortedCount);
            }
        }

        // Do nothing, this is just an incompatible subscriber
        static ValueTask HandleIncompatibleBatch(IAsyncEnumerable<TestEventTwo> eventsStream) => ValueTask.CompletedTask;

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 0; i < 10; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i, Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync(TimeSpan.FromMinutes(1));

        batchesCount.Should().BeGreaterThan(1);
        abortedCount.Should().BeGreaterOrEqualTo(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
        Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.Status.Should().Be(ConsumerStatus.Stopped);
    }
}
