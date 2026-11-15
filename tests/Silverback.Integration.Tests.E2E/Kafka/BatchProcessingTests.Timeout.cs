// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Logging;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class BatchProcessingTests
{
    [Fact]
    public async Task Batch_ShouldDeliverFirstMessage_WhenScopedSubscriberConstructionExceedsBatchTimeout()
    {
        TaskCompletionSource<bool> received = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(50)))))
            .AddScopedSubscriber(_ => new SlowBatchSubscriber(received)));

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());

        await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Batch_ShouldCompleteBatchAfterMaxWaitTimeFromFirstMessage_WhenMessagesKeepArriving()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(1000, TimeSpan.FromMilliseconds(500)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        // A message every 50 milliseconds, for at least 3 seconds: the batches are completed by the max wait time while the messages keep
        // arriving, instead of only once the messages stop
        for (int i = 1; i <= 60; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await Task.Delay(50);
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 60);
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedBatches.Count.ShouldBeGreaterThanOrEqualTo(3);
        receivedBatches.SelectMany(batch => batch).Select(message => message.ContentEventOne).ShouldBe(
            Enumerable.Range(1, 60).Select(i => $"{i}"));
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(60);
    }

    [Fact]
    public async Task Batch_ShouldCompletePendingBatchAfterTimeout()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(500)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(5);
        completedBatches.ShouldBe(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(5);
        completedBatches.ShouldBe(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(15);
    }

    [Fact]
    public async Task Batch_ShouldProcessAllConsumedMessagesAfterTimeout()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(500)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message);

                if (list.Count == 8)
                    await Task.Delay(500); // Wait past the timeout to process the remaining messages
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync(false, TimeSpan.FromSeconds(500));

        receivedBatches.Count.ShouldBeGreaterThanOrEqualTo(2);
        receivedBatches.All(list => list.Count <= 10).ShouldBeTrue();
        receivedBatches.Sum(batch => batch.Count).ShouldBe(15);
        completedBatches.ShouldBe(receivedBatches.Count);
    }

    [Fact]
    public async Task Batch_ShouldProcessAllConsumedMessagesWhenTimingOutWhileEnumerating()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(100, TimeSpan.FromMilliseconds(30)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message);

                if (list.Count == 20)
                    await Task.Delay(50);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 100; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync(false, TimeSpan.FromSeconds(500));

        receivedBatches.Count.ShouldBeGreaterThanOrEqualTo(5);
        receivedBatches.All(list => list.Count <= 30).ShouldBeTrue();
        receivedBatches.Sum(batch => batch.Count).ShouldBe(100);
        completedBatches.ShouldBe(receivedBatches.Count);
    }

    [Fact]
    [SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "Use ToList to make it blocking")]
    public async Task Batch_ShouldProcessAllConsumedMessagesWhenTimingOutWhileSynchronouslyEnumerating()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(100, TimeSpan.FromMilliseconds(50)))))
            .AddDelegateSubscriber<IEnumerable<TestEventOne>>(HandleBatch));

        void HandleBatch(IEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            foreach (TestEventOne message in batch)
            {
                list.Add(message);

                if (list.Count == 20)
                    Thread.Sleep(50);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 100; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync(false, TimeSpan.FromSeconds(500));

        receivedBatches.Count.ShouldBeGreaterThanOrEqualTo(5);
        receivedBatches.Sum(batch => batch.Count).ShouldBe(100);
        completedBatches.ShouldBe(receivedBatches.Count);
    }

    [Fact]
    public async Task Batch_ShouldCompletePendingBatchAfterTimeout_WhenTimeoutLongerThanDefaultInterval()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(10, TimeSpan.FromSeconds(2)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(5);
        completedBatches.ShouldBe(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(5);
        completedBatches.ShouldBe(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(15);
    }

    [Fact]
    public async Task Batch_ShouldNotOverlapNextSequence_WhenTimeoutElapses()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;
        int exitedSubscribers = 0;
        bool areOverlapping = false;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(200)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            if (completedBatches != exitedSubscribers)
                areOverlapping = true;

            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);

            await Task.Delay(500);

            Interlocked.Increment(ref exitedSubscribers);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 5);

        receivedBatches.Count.ShouldBe(1);
        receivedBatches[0].Count.ShouldBe(5);
        completedBatches.ShouldBe(0);

        await AsyncTestingUtil.WaitAsync(() => completedBatches == 1);

        completedBatches.ShouldBe(1);
        exitedSubscribers.ShouldBe(0);

        for (int i = 1; i <= 10; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        completedBatches.ShouldBeGreaterThan(1);
        exitedSubscribers.ShouldBeGreaterThan(1);
        areOverlapping.ShouldBeFalse();
    }

    [Fact]
    public async Task Batch_ShouldCommitAndConsumeNextBatch_WhenEnumerationIsAbortedWhileTimeoutIsCompletingBatch()
    {
        TestingCollection<string?> receivedMessages = [];
        int receivedBatches = 0;
        TestKafkaOffsetCommittedCallback offsetCommittedCallback = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(50)))))
            .AddSingletonBrokerClientCallback(offsetCommittedCallback)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        // Stop enumerating after processing the first message for longer than the timeout, which starts completing the batch meanwhile
        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            Interlocked.Increment(ref receivedBatches);

            await foreach (TestEventOne message in batch)
            {
                receivedMessages.Add(message.ContentEventOne);
                await Task.Delay(300);
                break;
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync();
        await AsyncTestingUtil.WaitAsync(() => offsetCommittedCallback.CallsCount >= 3);

        // Each message is consumed and committed exactly once
        receivedBatches.ShouldBe(3);
        receivedMessages.ShouldBe(["1", "2", "3"]);
        offsetCommittedCallback.CallsCount.ShouldBe(3);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);
    }

    [Fact]
    public async Task Batch_ShouldAddMessageToNextBatch_WhenRefusedByTimedOutBatchThatIsSkipped()
    {
        TestingCollection<List<string?>> successfulBatches = [];
        BatchTimeoutGate batchTimeoutGate = new();
        LogEventsCollector logEventsCollector = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddSingleton(batchTimeoutGate)
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(10, TimeSpan.FromSeconds(1))
                        .OnError(policy => policy.Skip()))))
            .AddTransientSequenceReader<GatedBatchSequenceReader>()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);
            }

            // Skip the first batch
            if (list is ["1", "2"])
                throw new InvalidOperationException("Skip!");

            successfulBatches.Add(list);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await batchTimeoutGate.ProduceRefusedMessageAsync(
            () => producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" }).AsTask(),
            logEventsCollector);

        // The refused message isn't committed with the skipped batch, but added to the next one
        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Any(batch => batch.Contains("3")));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        successfulBatches.SelectMany(batch => batch).ShouldBe(["3"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);
    }

    [Fact]
    public async Task Batch_ShouldProcessRefusedMessageOnce_WhenTimedOutBatchIsRetried()
    {
        TestingCollection<List<string?>> successfulBatches = [];
        BatchTimeoutGate batchTimeoutGate = new();
        LogEventsCollector logEventsCollector = new();
        int failedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddSingleton(batchTimeoutGate)
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(10, TimeSpan.FromSeconds(1))
                        .OnError(policy => policy.Retry(1)))))
            .AddTransientSequenceReader<GatedBatchSequenceReader>()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);
            }

            // Fail the first batch once
            if (list is ["1", "2"] && Interlocked.Increment(ref failedBatches) == 1)
                throw new InvalidOperationException("Retry!");

            successfulBatches.Add(list);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await batchTimeoutGate.ProduceRefusedMessageAsync(
            () => producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" }).AsTask(),
            logEventsCollector);

        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Any(batch => batch.Contains("3")));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The refused message is consumed again after the seek and processed once
        failedBatches.ShouldBe(1);
        successfulBatches.SelectMany(batch => batch).ShouldBe(["1", "2", "3"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);
    }

    private sealed class SlowBatchSubscriber
    {
        private readonly TaskCompletionSource<bool> _received;

        public SlowBatchSubscriber(TaskCompletionSource<bool> received)
        {
            _received = received;
            Thread.Sleep(100);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Used by Silverback")]
        public void Execute(IEnumerable<TestEventOne> messages)
        {
            if (messages.Any())
                _received.TrySetResult(true);
        }
    }
}
