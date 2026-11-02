// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Logging;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class ConsumerTests : KafkaTests
{
    public ConsumerTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task PauseAndResume_ShouldPauseAndResumeConsuming()
    {
        int[] receivedMessages = [0, 0, 0];
        TopicPartition pausedPartition = new(DefaultTopicName, 1);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<IInboundEnvelope<TestEventWithKafkaKey>>(HandleEnvelope));

        void HandleEnvelope(IInboundEnvelope<TestEventWithKafkaKey> envelope)
        {
            KafkaOffset offset = (KafkaOffset)envelope.BrokerMessageIdentifier;
            receivedMessages[offset.TopicPartition.Partition]++;
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();
        consumer.Pause([pausedPartition]);

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = $"{i}" });
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = $"{i}" });
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 3, Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Sum() == 6);
        receivedMessages.Sum().ShouldBe(6);
        receivedMessages[pausedPartition.Partition].ShouldBe(0); // First + message ready in queue

        consumer.Resume([pausedPartition]);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        receivedMessages.Sum().ShouldBe(9);
        receivedMessages[0].ShouldBe(3);
        receivedMessages[1].ShouldBe(3);
        receivedMessages[2].ShouldBe(3);
    }

    [Fact]
    public async Task Seek_ShouldRepositionOffset()
    {
        int[] receivedMessages = [0, 0, 0];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<IInboundEnvelope<TestEventWithKafkaKey>>(HandleEnvelope));

        void HandleEnvelope(IInboundEnvelope<TestEventWithKafkaKey> envelope)
        {
            KafkaOffset offset = (KafkaOffset)envelope.BrokerMessageIdentifier;
            receivedMessages[offset.TopicPartition.Partition]++;
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = $"{i}" });
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = $"{i}" });
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 3, Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        receivedMessages.Sum().ShouldBe(9);
        receivedMessages[0].ShouldBe(3);
        receivedMessages[1].ShouldBe(3);
        receivedMessages[2].ShouldBe(3);

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();
        consumer.Seek(new TopicPartitionOffset(DefaultTopicName, 1, 0));

        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Sum() >= 12);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        receivedMessages.Sum().ShouldBe(12);
        receivedMessages[1].ShouldBe(6);
    }

    [Fact]
    public async Task StopAsync_ShouldCancelProcessing()
    {
        bool received = false;
        bool canceled = false;
        using CancellationTokenSource antiDeadlockCancellationTokenSource = new();
        using SemaphoreSlim semaphore = new(0);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<TestEventOne, CancellationToken>(HandleEventAsync));

        async Task HandleEventAsync(TestEventOne message, CancellationToken cancellationToken)
        {
            received = true;

            while (!cancellationToken.IsCancellationRequested && !antiDeadlockCancellationTokenSource.IsCancellationRequested)
            {
                await Task.Delay(10, CancellationToken.None);
            }

            canceled = true;

            await semaphore.WaitAsync(CancellationToken.None);
        }

        try
        {
            KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();

            IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
            await producer.ProduceAsync(new TestEventOne(), cancellationToken: CancellationToken.None);

            await AsyncTestingUtil.WaitAsync(() => received);
            received.ShouldBeTrue();

            consumer.StopAsync().FireAndForget();

            await AsyncTestingUtil.WaitAsync(() => canceled);
            canceled.ShouldBeTrue();
            consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);

            semaphore.Release();
            await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Stopped);
            consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        }
        finally
        {
            antiDeadlockCancellationTokenSource.Cancel();
            semaphore.Release(); // in any case, always release to avoid deadlocking
        }
    }

    [Fact]
    public async Task StopAsync_ShouldBlockUntilProcessingFinished()
    {
        bool done = false;
        SemaphoreSlim semaphore = new(0);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<TestEventOne>(HandleEventAsync));

        async Task HandleEventAsync(TestEventOne message)
        {
            semaphore.Release();
            await Task.Delay(500, CancellationToken.None); // Ignore cancellation
            done = true;
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();

        await producer.ProduceAsync(new TestEventOne());

        await semaphore.WaitAsync();
        await consumer.StopAsync();
        done.ShouldBeTrue();
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
    }

    [Fact]
    public async Task StopAsync_ShouldBlockUntilBatchProcessingFinished()
    {
        bool done = false;
        SemaphoreSlim semaphore = new(0);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).EnableBatchProcessing(3))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleEventAsync));

        async Task HandleEventAsync(IAsyncEnumerable<TestEventOne> messages)
        {
            await messages.ToListAsync();
            semaphore.Release();
            await Task.Delay(500, CancellationToken.None); // Ignore cancellation
            done = true;
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await semaphore.WaitAsync();
        await consumer.StopAsync();
        done.ShouldBeTrue();
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
    }

    [Fact]
    public async Task StopAndStart_ShouldConsumeBufferedMessagesAgain()
    {
        TestingCollection<int> receivedMessages = [];
        TaskCompletionSource<bool> firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LogEventsCollector logEventsCollector = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<TestEventOne>(HandleEventAsync));

        async Task HandleEventAsync(TestEventOne message)
        {
            int value = int.Parse(message.ContentEventOne!, CultureInfo.InvariantCulture);
            receivedMessages.Add(value);

            if (value != 0)
                return;

            firstStarted.TrySetResult(true);
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        try
        {
            for (int i = 0; i < 5; i++)
            {
                await producer.ProduceAsync(new TestEventOne { ContentEventOne = i.ToString(CultureInfo.InvariantCulture) });
            }

            await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // The other messages are fetched and buffered while the first one is being processed
            await AsyncTestingUtil.WaitAsync(() =>
                logEventsCollector.Contains(KafkaLogEvents.ConsumingMessage.EventId, $"{DefaultTopicName}[0]@4"));
            logEventsCollector.Contains(KafkaLogEvents.ConsumingMessage.EventId, $"{DefaultTopicName}[0]@4").ShouldBeTrue();

            Task stop = consumer.StopAsync().AsTask();
            release.TrySetResult(true);
            await stop.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            release.TrySetResult(true);
        }

        await consumer.StartAsync();
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "5" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedMessages.ShouldBe([0, 1, 2, 3, 4, 5]);
        DefaultConsumerGroup.GetCommittedOffset(new TopicPartition(DefaultTopicName, 0))!.Offset.Value.ShouldBe(6);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StopAndStart_ShouldConsumeEveryMessage_WhenRebalanceOccursWhileStopped(bool waitForRevocation)
    {
        TestingCollection<int> receivedMessages = [];
        TaskCompletionSource<bool> firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LogEventsCollector logEventsCollector = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<TestEventOne>(HandleEventAsync));

        async Task HandleEventAsync(TestEventOne message)
        {
            int value = int.Parse(message.ContentEventOne!, CultureInfo.InvariantCulture);
            receivedMessages.Add(value);

            if (value != 0)
                return;

            firstStarted.TrySetResult(true);
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        try
        {
            for (int i = 0; i < 5; i++)
            {
                await producer.ProduceAsync(new TestEventOne { ContentEventOne = i.ToString(CultureInfo.InvariantCulture) });
            }

            await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await AsyncTestingUtil.WaitAsync(() =>
                logEventsCollector.Contains(KafkaLogEvents.ConsumingMessage.EventId, $"{DefaultTopicName}[0]@4"));
            logEventsCollector.Contains(KafkaLogEvents.ConsumingMessage.EventId, $"{DefaultTopicName}[0]@4").ShouldBeTrue();

            Task stop = consumer.StopAsync().AsTask();
            release.TrySetResult(true);
            await stop.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            release.TrySetResult(true);
        }

        // The restart seeks back to the first uncommitted message, unless the partition is revoked first: the reassignment then
        // resumes from the committed offset
        DefaultConsumerGroup.Rebalance();

        if (waitForRevocation)
        {
            await AsyncTestingUtil.WaitAsync(() =>
                logEventsCollector.Contains(KafkaLogEvents.PartitionRevoked.EventId, $"{DefaultTopicName}[0]"));
            logEventsCollector.Contains(KafkaLogEvents.PartitionRevoked.EventId, $"{DefaultTopicName}[0]").ShouldBeTrue();
        }

        await consumer.StartAsync();
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "5" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedMessages.Distinct().Order().ShouldBe([0, 1, 2, 3, 4, 5]);
        DefaultConsumerGroup.GetCommittedOffset(new TopicPartition(DefaultTopicName, 0))!.Offset.Value.ShouldBe(6);
    }

    [Fact]
    public async Task StopAndStart_ShouldConsumeBufferedMessagesAgain_WhenProcessingAllPartitionsTogether()
    {
        ConcurrentDictionary<int, TestingCollection<int>> receivedMessages = new();
        int receivedCount = 0;
        TaskCompletionSource<bool> firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LogEventsCollector logEventsCollector = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .ProcessAllPartitionsTogether()
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<IInboundEnvelope<TestEventWithKafkaKey>>(HandleEnvelopeAsync));

        async Task HandleEnvelopeAsync(IInboundEnvelope<TestEventWithKafkaKey> envelope)
        {
            KafkaOffset offset = (KafkaOffset)envelope.BrokerMessageIdentifier;
            receivedMessages
                .GetOrAdd(offset.TopicPartition.Partition.Value, _ => [])
                .Add(int.Parse(envelope.Message!.Content!, CultureInfo.InvariantCulture));

            if (Interlocked.Increment(ref receivedCount) != 1)
                return;

            firstStarted.TrySetResult(true);
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        try
        {
            // The keys 1, 2 and 3 are routed to different partitions
            for (int i = 0; i < 5; i++)
            {
                await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = i.ToString(CultureInfo.InvariantCulture) });
                await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = i.ToString(CultureInfo.InvariantCulture) });
                await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 3, Content = i.ToString(CultureInfo.InvariantCulture) });
            }

            await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // The other messages are fetched and buffered while the first one is being processed
            await AsyncTestingUtil.WaitAsync(() => Enumerable.Range(0, 3).All(partition =>
                logEventsCollector.Contains(KafkaLogEvents.ConsumingMessage.EventId, $"{DefaultTopicName}[{partition}]@4")));

            for (int partition = 0; partition < 3; partition++)
            {
                logEventsCollector.Contains(KafkaLogEvents.ConsumingMessage.EventId, $"{DefaultTopicName}[{partition}]@4").ShouldBeTrue();
            }

            Task stop = consumer.StopAsync().AsTask();
            release.TrySetResult(true);
            await stop.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            release.TrySetResult(true);
        }

        await consumer.StartAsync();
        await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = "5" });
        await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = "5" });
        await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 3, Content = "5" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedMessages.Keys.Order().ShouldBe([0, 1, 2]);

        for (int partition = 0; partition < 3; partition++)
        {
            receivedMessages[partition].ShouldBe([0, 1, 2, 3, 4, 5]);
            DefaultConsumerGroup.GetCommittedOffset(new TopicPartition(DefaultTopicName, partition))!.Offset.Value.ShouldBe(6);
        }
    }
}
