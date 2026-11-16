// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

// These tests lower the thread pool min threads, which is a process-wide setting: the non-parallel collection runs them alone
[Collection(nameof(ThreadPoolStarvationTests))]
[CollectionDefinition(nameof(ThreadPoolStarvationTests), DisableParallelization = true)]
public class ThreadPoolStarvationTests : KafkaTests
{
    private const int BatchSize = 5;

    // The thread pool quickly compensates for the threads blocked waiting on a task (as the enumerables do), so only the
    // observable, which blocks on a semaphore, reliably starves it without the dedicated threads (with more partitions than
    // the thread pool grows to in time, hence the scaling with the cores). The other tests cover the same path end to end.
    private static readonly int PartitionsCount = 8 * Environment.ProcessorCount;

    private static readonly int MessagesCount = PartitionsCount * BatchSize;

    private readonly TestingCollection<TestEventOne> _receivedMessages = [];

    public ThreadPoolStarvationTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public Task Batch_ShouldNotStarveThreadPool_WhenSubscribingToEnumerable() =>
        ConsumeBatchesWithConstrainedThreadPoolAsync(silverback => silverback
            .AddDelegateSubscriber<IEnumerable<TestEventOne>>(HandleEnumerable));

    [Fact]
    public Task Batch_ShouldNotStarveThreadPool_WhenEnumeratingMessageStreamSynchronously() =>
        ConsumeBatchesWithConstrainedThreadPoolAsync(silverback => silverback
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleMessageStreamEnumerable));

    [Fact]
    public Task Batch_ShouldNotStarveThreadPool_WhenSubscribingToObservable() =>
        ConsumeBatchesWithConstrainedThreadPoolAsync(silverback => silverback
            .AsObservable()
            .AddDelegateSubscriber<IObservable<TestEventOne>>(HandleObservable));

    [Fact]
    public Task Batch_ShouldNotStarveThreadPool_WhenSubscribingToAsyncEnumerable() =>
        ConsumeBatchesWithConstrainedThreadPoolAsync(silverback => silverback
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleAsyncEnumerableAsync));

    // Each batch subscriber that consumes its batch synchronously blocks its thread until the batch is complete. With a batch
    // per partition and few thread pool threads, they would starve the thread pool, including the continuations pushing the
    // messages into the batches, unless they run on a dedicated thread.
    private async Task ConsumeBatchesWithConstrainedThreadPoolAsync(Func<SilverbackBuilder, SilverbackBuilder> addSubscriber)
    {
        ThreadPool.GetMinThreads(out int minWorkerThreads, out int minCompletionPortThreads);
        ThreadPool.SetMinThreads(4, 4).ShouldBeTrue();

        try
        {
            await Host.ConfigureServicesAndRunAsync(services => addSubscriber(services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(PartitionsCount)))
                .AddKafkaClients(clients => clients
                    .WithBootstrapServers("PLAINTEXT://e2e")
                    .AddConsumer(consumer => consumer
                        .WithGroupId(DefaultGroupId)
                        .LimitParallelism(PartitionsCount)
                        .CommitOffsetEach(1)
                        .Consume<TestEventOne>(endpoint => endpoint
                            .ConsumeFrom(DefaultTopicName)
                            .EnableBatchProcessing(BatchSize))))));

            IProducer producer = Helper.GetProducer(producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(endpoint => endpoint
                    .ProduceTo(DefaultTopicName, message => int.Parse(message!.ContentEventOne!, CultureInfo.InvariantCulture))));

            Stopwatch stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < MessagesCount; i++)
            {
                await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i % PartitionsCount}" });
            }

            await AsyncTestingUtil.WaitAsync(() => _receivedMessages.Count >= MessagesCount, TimeSpan.FromSeconds(30));

            // Measure the elapsed time, since a starving thread pool also slows down the polling of the wait
            stopwatch.Stop();
            _receivedMessages.Count.ShouldBe(MessagesCount);
            stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(10));
        }
        finally
        {
            ThreadPool.SetMinThreads(minWorkerThreads, minCompletionPortThreads);
        }
    }

    private void HandleEnumerable(IEnumerable<TestEventOne> batch)
    {
        foreach (TestEventOne message in batch)
        {
            _receivedMessages.Add(message);
        }
    }

    private void HandleMessageStreamEnumerable(IMessageStreamEnumerable<TestEventOne> batch)
    {
        foreach (TestEventOne message in batch)
        {
            _receivedMessages.Add(message);
        }
    }

    private void HandleObservable(IObservable<TestEventOne> batch) => batch.Subscribe(_receivedMessages.Add);

    private async Task HandleAsyncEnumerableAsync(IAsyncEnumerable<TestEventOne> batch)
    {
        await foreach (TestEventOne message in batch)
        {
            _receivedMessages.Add(message);
        }
    }
}
