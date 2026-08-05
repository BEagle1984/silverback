// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class BatchProcessingTests
{
    /// <summary>
    ///     Demonstrates ThreadPool starvation when using synchronous IEnumerable subscribers
    ///     with many partitions. Each partition's batch subscriber blocks a ThreadPool thread
    ///     in SafeWait (MessageStreamEnumerable.GetEnumerable:149), eventually starving
    ///     async continuations needed to push messages and drain channels.
    /// </summary>
    [Fact]
    public async Task Batch_ShouldStarveThreadPool_WhenSyncEnumerableWithManyPartitions()
    {
        const int partitionCount = 24;
        const int batchSize = 5;
        const int messagesPerPartition = batchSize; // One full batch per partition
        const int totalMessages = partitionCount * messagesPerPartition;

        // Constrain the ThreadPool to simulate real-world conditions.
        // The base class sets MinThreads = 4 * ProcessorCount to prevent starvation in other tests.
        // Here we intentionally lower it to expose the bug.
        ThreadPool.GetMinThreads(out int originalMinWorker, out int originalMinIo);
        ThreadPool.SetMinThreads(4, 4); // Typical default on a 4-core machine

        try
        {
            TestingCollection<List<TestEventOne>> receivedBatches = [];
            int completedBatches = 0;

            await Host.ConfigureServicesAndRunAsync(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(partitionCount)))
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
                                            .EnableBatchProcessing(batchSize))))
                    .AddDelegateSubscriber<IEnumerable<TestEventOne>>(HandleBatch));

            void HandleBatch(IEnumerable<TestEventOne> batch)
            {
                List<TestEventOne> list = [];
                receivedBatches.Add(list);

                foreach (TestEventOne message in batch)
                {
                    list.Add(message);
                }

                Interlocked.Increment(ref completedBatches);
            }

            // Produce messages distributed across all partitions via kafka key
            IProducer producer = Helper.GetProducer(
                producer => producer
                    .WithBootstrapServers("PLAINTEXT://e2e")
                    .Produce<TestEventOne>(
                        endpoint => endpoint
                            .ProduceTo(DefaultTopicName)
                            .SetKafkaKey(envelope => envelope.Message?.ContentEventOne)));

            for (int i = 0; i < totalMessages; i++)
            {
                // Use partition index as key to distribute evenly across all partitions
                await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"p{i % partitionCount}" });
            }

            // Wait for all messages to be consumed, with a short timeout.
            // Under starvation this will time out because ThreadPool threads are all blocked
            // in synchronous SafeWait and async continuations (PushAsync, channel drain) can't run.
            bool allConsumed = false;
            await AsyncTestingUtil.WaitAsync(
                () => allConsumed = receivedBatches.Sum(batch => batch.Count) == totalMessages,
                TimeSpan.FromSeconds(30));

            // If all messages were consumed within the timeout, the system didn't starve
            // (e.g., because the ThreadPool grew fast enough). This is the expected behavior
            // when the fix is applied. If the test times out or not all messages are consumed,
            // the starvation is confirmed.
            if (!allConsumed)
            {
                int received = receivedBatches.Sum(batch => batch.Count);
                int pending = totalMessages - received;
                ThreadPool.GetAvailableThreads(out int availableWorker, out _);
                ThreadPool.GetMaxThreads(out int maxWorker, out _);
                ThreadPool.GetMinThreads(out int minWorker, out _);

                throw new TimeoutException(
                    $"ThreadPool starvation detected: only {received}/{totalMessages} messages consumed " +
                    $"({completedBatches} batches completed) after 30s timeout. " +
                    $"ThreadPool state: min={minWorker}, available={availableWorker}, max={maxWorker}. " +
                    $"With {partitionCount} partitions and sync IEnumerable subscribers, " +
                    $"{partitionCount} ThreadPool threads are permanently blocked in SafeWait " +
                    $"at MessageStreamEnumerable.GetEnumerable(), starving async continuations. " +
                    $"Pending messages: {pending}");
            }
        }
        finally
        {
            // Restore the ThreadPool min threads for other tests
            ThreadPool.SetMinThreads(originalMinWorker, originalMinIo);
        }
    }

    /// <summary>
    ///     Control test: same scenario with IAsyncEnumerable subscriber, which does NOT
    ///     block ThreadPool threads. Should always succeed regardless of ThreadPool size.
    /// </summary>
    [Fact]
    public async Task Batch_ShouldNotStarveThreadPool_WhenAsyncEnumerableWithManyPartitions()
    {
        const int partitionCount = 24;
        const int batchSize = 5;
        const int messagesPerPartition = batchSize;
        const int totalMessages = partitionCount * messagesPerPartition;

        ThreadPool.GetMinThreads(out int originalMinWorker, out int originalMinIo);
        ThreadPool.SetMinThreads(4, 4);

        try
        {
            TestingCollection<List<TestEventOne>> receivedBatches = [];
            int completedBatches = 0;

            await Host.ConfigureServicesAndRunAsync(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(partitionCount)))
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
                                            .EnableBatchProcessing(batchSize))))
                    .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

            async Task HandleBatch(IAsyncEnumerable<TestEventOne> batch)
            {
                List<TestEventOne> list = [];
                receivedBatches.Add(list);

                await foreach (TestEventOne message in batch)
                {
                    list.Add(message);
                }

                Interlocked.Increment(ref completedBatches);
            }

            IProducer producer = Helper.GetProducer(
                producer => producer
                    .WithBootstrapServers("PLAINTEXT://e2e")
                    .Produce<TestEventOne>(
                        endpoint => endpoint
                            .ProduceTo(DefaultTopicName)
                            .SetKafkaKey(envelope => envelope.Message?.ContentEventOne)));

            for (int i = 0; i < totalMessages; i++)
            {
                await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"p{i % partitionCount}" });
            }

            await AsyncTestingUtil.WaitAsync(
                () => receivedBatches.Sum(batch => batch.Count) == totalMessages,
                TimeSpan.FromSeconds(30));

            receivedBatches.Sum(batch => batch.Count).ShouldBe(totalMessages);
            completedBatches.ShouldBe(partitionCount);
        }
        finally
        {
            ThreadPool.SetMinThreads(originalMinWorker, originalMinIo);
        }
    }
}
