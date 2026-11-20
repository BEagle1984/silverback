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
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class CatchUpTests : KafkaTests
{
    private static readonly TimeSpan CaughtUpTimeout = TimeSpan.FromSeconds(10);

    public CatchUpTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task CatchUp_ShouldNotifyOnceAllMessagesAreProcessed()
    {
        int processedCount = 0;
        CaughtUpCallback caughtUpCallback = new(() => processedCount);

        await RunHostAsync(
            3,
            consumer => consumer
                .NotifyWhenCaughtUp()
                .DisableOffsetsCommit()
                .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFromBeginning(DefaultTopicName)),
            caughtUpCallback,
            silverback => silverback.AddDelegateSubscriber<TestEventWithKafkaKey>(HandleMessageAsync));

        async Task HandleMessageAsync(TestEventWithKafkaKey message)
        {
            await Task.Delay(10);
            Interlocked.Increment(ref processedCount);
        }

        await ProduceAsync(15);

        await ConnectAndWaitUntilCaughtUpAsync(caughtUpCallback);

        caughtUpCallback.CallsCount.ShouldBe(1);
        caughtUpCallback.ProcessedCountWhenCaughtUp.ShouldBe(15);
        processedCount.ShouldBe(15);
    }

    [Fact]
    public async Task CatchUp_ShouldCompletePendingBatchAtEndOfPartition()
    {
        int processedCount = 0;
        int batchesCount = 0;
        CaughtUpCallback caughtUpCallback = new(() => processedCount);

        await RunHostAsync(
            3,
            consumer => consumer
                .NotifyWhenCaughtUp()
                .DisableOffsetsCommit()
                .Consume<TestEventWithKafkaKey>(endpoint => endpoint
                    .ConsumeFromBeginning(DefaultTopicName)
                    .EnableBatchProcessing(100)),
            caughtUpCallback,
            silverback => silverback.AddDelegateSubscriber<IMessageStreamEnumerable<TestEventWithKafkaKey>>(HandleBatchAsync));

        async Task HandleBatchAsync(IMessageStreamEnumerable<TestEventWithKafkaKey> batch)
        {
            await foreach (TestEventWithKafkaKey dummy in batch)
            {
                Interlocked.Increment(ref processedCount);
            }

            Interlocked.Increment(ref batchesCount);
        }

        await ProduceAsync(15);

        await ConnectAndWaitUntilCaughtUpAsync(caughtUpCallback);

        caughtUpCallback.CallsCount.ShouldBe(1);
        caughtUpCallback.ProcessedCountWhenCaughtUp.ShouldBe(15);
        batchesCount.ShouldBe(3);
    }

    [Fact]
    public async Task CatchUp_ShouldNotCompleteBatchesEarly_WhenPartitionIsCaughtUpWhileAnotherIsLagging()
    {
        ConcurrentDictionary<int, ConcurrentQueue<int>> batchSizes = new();
        TaskCompletionSource laggingPartitionGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CaughtUpCallback caughtUpCallback = new(() => 0);

        await RunHostAsync(
            2,
            consumer => consumer
                .NotifyWhenCaughtUp()
                .DisableOffsetsCommit()
                .Consume<TestEventOne>(endpoint => endpoint
                    .ConsumeFromBeginning(DefaultTopicName)
                    .EnableBatchProcessing(3, TimeSpan.FromSeconds(30))),
            caughtUpCallback,
            silverback => silverback.AddDelegateSubscriber<IMessageStreamEnumerable<IInboundEnvelope<TestEventOne>>>(HandleBatchAsync));

        async Task HandleBatchAsync(IMessageStreamEnumerable<IInboundEnvelope<TestEventOne>> batch)
        {
            int partition = -1;
            int count = 0;

            await foreach (IInboundEnvelope<TestEventOne> envelope in batch)
            {
                partition = ((KafkaOffset)envelope.BrokerMessageIdentifier).TopicPartition.Partition.Value;
                count++;
            }

            batchSizes.GetOrAdd(partition, _ => new ConcurrentQueue<int>()).Enqueue(count);

            if (partition == 0)
                await laggingPartitionGate.Task;
        }

        await ProduceToPartitionAsync(0, 3);
        await ProduceToPartitionAsync(1, 1);

        IKafkaConsumer consumer = (IKafkaConsumer)Helper.GetConsumer("consumer");
        await consumer.Client.ConnectAsync();

        await AsyncTestingUtil.WaitAsync(() => batchSizes.ContainsKey(0) && batchSizes.ContainsKey(1));

        try
        {
            // Partition 1 is caught up: its new records are batched as usual, until the batch is full or its max wait time elapses
            await ProduceToPartitionAsync(1, 2);
            await Task.Delay(300);

            batchSizes[1].ShouldBe([1]);
        }
        finally
        {
            // Released on failure as well, otherwise the blocked subscriber prevents the host from being disposed
            laggingPartitionGate.TrySetResult();
        }

        await caughtUpCallback.WaitUntilInvokedAsync(1);

        caughtUpCallback.CallsCount.ShouldBe(1);
        batchSizes[1].ShouldBe([1]);
    }

    [Fact]
    public async Task CatchUp_ShouldNotifyOnceBatchFailedAtEndOfPartitionIsRetried()
    {
        int processedCount = 0;
        int attemptsCount = 0;
        CaughtUpCallback caughtUpCallback = new(() => processedCount);

        await RunHostAsync(
            1,
            consumer => consumer
                .NotifyWhenCaughtUp()
                .DisableOffsetsCommit()
                .Consume<TestEventWithKafkaKey>(endpoint => endpoint
                    .ConsumeFromBeginning(DefaultTopicName)
                    .EnableBatchProcessing(100)
                    .OnError(policy => policy.Retry(1))),
            caughtUpCallback,
            silverback => silverback.AddDelegateSubscriber<IMessageStreamEnumerable<TestEventWithKafkaKey>>(HandleBatchAsync));

        async Task HandleBatchAsync(IMessageStreamEnumerable<TestEventWithKafkaKey> batch)
        {
            int count = 0;

            await foreach (TestEventWithKafkaKey dummy in batch)
            {
                count++;
            }

            if (Interlocked.Increment(ref attemptsCount) == 1)
                throw new InvalidOperationException("Test failure");

            Interlocked.Add(ref processedCount, count);
        }

        await ProduceAsync(15);

        await ConnectAndWaitUntilCaughtUpAsync(caughtUpCallback);

        caughtUpCallback.CallsCount.ShouldBe(1);
        caughtUpCallback.ProcessedCountWhenCaughtUp.ShouldBe(15);
        attemptsCount.ShouldBe(2);
    }

    [Fact]
    public async Task CatchUp_ShouldNotifyOnceRecordsBeforeIncompleteChunkedMessageAreProcessed()
    {
        int processedCount = 0;
        CaughtUpCallback caughtUpCallback = new(() => processedCount);
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(new TestEventOne { ContentEventOne = "Incomplete" });

        await RunHostAsync(
            1,
            consumer => consumer
                .NotifyWhenCaughtUp()
                .DisableOffsetsCommit()
                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFromBeginning(DefaultTopicName)),
            caughtUpCallback,
            silverback => silverback.AddDelegateSubscriber<TestEventOne>(HandleMessage));

        void HandleMessage(TestEventOne message) => Interlocked.Increment(ref processedCount);

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Complete" });
        await producer.RawProduceAsync(
            [.. rawMessage.Take(10)],
            HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            [.. rawMessage.Skip(10).Take(10)],
            HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));

        await ConnectAndWaitUntilCaughtUpAsync(caughtUpCallback);

        caughtUpCallback.CallsCount.ShouldBe(1);
        caughtUpCallback.ProcessedCountWhenCaughtUp.ShouldBe(1);
    }

    [Fact]
    public async Task CatchUp_ShouldNotifyOnceAllMessagesAreRead_WhenStreaming()
    {
        int readCount = 0;
        CaughtUpCallback caughtUpCallback = new(() => readCount);

        await RunHostAsync(
            3,
            consumer => consumer
                .NotifyWhenCaughtUp()
                .DisableOffsetsCommit()
                .Consume<TestEventWithKafkaKey>(endpoint => endpoint
                    .ConsumeFromBeginning(DefaultTopicName)
                    .AllowStreaming()),
            caughtUpCallback,
            silverback => silverback.AddDelegateSubscriber<IMessageStreamEnumerable<TestEventWithKafkaKey>>(HandleStreamAsync));

        async Task HandleStreamAsync(IMessageStreamEnumerable<TestEventWithKafkaKey> stream)
        {
            await foreach (TestEventWithKafkaKey dummy in stream)
            {
                Interlocked.Increment(ref readCount);
            }
        }

        await ProduceAsync(15);

        await ConnectAndWaitUntilCaughtUpAsync(caughtUpCallback);

        caughtUpCallback.CallsCount.ShouldBe(1);
        caughtUpCallback.ProcessedCountWhenCaughtUp.ShouldBe(15);
    }

    [Fact]
    public async Task CatchUp_ShouldNotifyRightAway_WhenTopicIsEmpty()
    {
        CaughtUpCallback caughtUpCallback = new(() => 0);

        await RunHostAsync(
            3,
            consumer => consumer
                .NotifyWhenCaughtUp()
                .DisableOffsetsCommit()
                .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFromBeginning(DefaultTopicName)),
            caughtUpCallback);

        await ConnectAndWaitUntilCaughtUpAsync(caughtUpCallback);

        caughtUpCallback.CallsCount.ShouldBe(1);
    }

    [Fact]
    public async Task CatchUp_ShouldKeepConsumingWithoutNotifyingAgain_WhenCaughtUp()
    {
        int processedCount = 0;
        CaughtUpCallback caughtUpCallback = new(() => processedCount);

        await RunHostAsync(
            3,
            consumer => consumer
                .NotifyWhenCaughtUp()
                .DisableOffsetsCommit()
                .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFromBeginning(DefaultTopicName)),
            caughtUpCallback,
            silverback => silverback.AddDelegateSubscriber<TestEventWithKafkaKey>(HandleMessage));

        void HandleMessage(TestEventWithKafkaKey message) => Interlocked.Increment(ref processedCount);

        await ProduceAsync(15);

        IKafkaConsumer consumer = await ConnectAndWaitUntilCaughtUpAsync(caughtUpCallback);

        await ProduceAsync(5);

        await AsyncTestingUtil.WaitAsync(() => processedCount == 20);

        processedCount.ShouldBe(20);
        caughtUpCallback.CallsCount.ShouldBe(1);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
    }

    [Fact]
    public async Task CatchUp_ShouldNotifyAgain_WhenReconnectedByAutoRecovery()
    {
        CaughtUpCallback caughtUpCallback = new(() => 0);

        await RunHostAsync(
            3,
            consumer => consumer
                .NotifyWhenCaughtUp()
                .DisableOffsetsCommit()
                .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFromBeginning(DefaultTopicName)),
            caughtUpCallback,
            silverback => silverback.AddDelegateSubscriber<TestEventWithKafkaKey>(HandleMessage));

        static void HandleMessage(TestEventWithKafkaKey message)
        {
            // Consumed only to catch up
        }

        await ProduceAsync(15);

        IKafkaConsumer consumer = await ConnectAndWaitUntilCaughtUpAsync(caughtUpCallback);

        await consumer.TriggerReconnectAsync();

        await caughtUpCallback.WaitUntilInvokedAsync(2);
        await consumer.WaitUntilCaughtUpAsync().WaitAsync(CaughtUpTimeout, TestContext.Current.CancellationToken);

        caughtUpCallback.CallsCount.ShouldBe(2);
    }

    [Fact]
    public async Task CatchUp_ShouldNotifyOnceAllMessagesAreProcessed_WhenSubscribingWithGroup()
    {
        int processedCount = 0;
        CaughtUpCallback caughtUpCallback = new(() => processedCount);

        await RunHostAsync(
            3,
            consumer => consumer
                .WithGroupId(DefaultGroupId)
                .AutoResetOffsetToEarliest()
                .NotifyWhenCaughtUp()
                .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)),
            caughtUpCallback,
            silverback => silverback.AddDelegateSubscriber<TestEventWithKafkaKey>(HandleMessageAsync));

        async Task HandleMessageAsync(TestEventWithKafkaKey message)
        {
            await Task.Delay(10);
            Interlocked.Increment(ref processedCount);
        }

        await ProduceAsync(15);

        await ConnectAndWaitUntilCaughtUpAsync(caughtUpCallback);

        caughtUpCallback.CallsCount.ShouldBe(1);
        caughtUpCallback.ProcessedCountWhenCaughtUp.ShouldBe(15);
    }

    [Fact]
    public async Task CatchUp_ShouldNotNotify_WhenNoPartitionIsAssignedInGroup()
    {
        CaughtUpCallback caughtUpCallback = new(() => 0);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .ManuallyConnect())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(
                    "consumer1",
                    consumer => consumer
                        .WithGroupId(DefaultGroupId)
                        .NotifyWhenCaughtUp()
                        .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                .AddConsumer(
                    "consumer2",
                    consumer => consumer
                        .WithGroupId(DefaultGroupId)
                        .NotifyWhenCaughtUp()
                        .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddSingletonBrokerClientCallback(caughtUpCallback));

        IKafkaConsumer consumer1 = (IKafkaConsumer)Helper.GetConsumer("consumer1");
        IKafkaConsumer consumer2 = (IKafkaConsumer)Helper.GetConsumer("consumer2");
        await consumer1.Client.ConnectAsync();
        await consumer2.Client.ConnectAsync();

        await caughtUpCallback.WaitUntilInvokedAsync(1);
        await AsyncTestingUtil.WaitAsync(() => consumer1.Client.Assignment.Count + consumer2.Client.Assignment.Count == 1);
        await Task.Delay(200);

        caughtUpCallback.CallsCount.ShouldBe(1);
        caughtUpCallback.CaughtUpConsumers.Single().Client.Assignment.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CatchUp_ShouldDisconnectOnceCaughtUpAndCatchUpAgainWhenReconnected()
    {
        int processedCount = 0;
        CaughtUpCallback caughtUpCallback = new(() => processedCount);

        await RunHostAsync(
            3,
            consumer => consumer
                .DisconnectWhenCaughtUp()
                .DisableOffsetsCommit()
                .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFromBeginning(DefaultTopicName)),
            caughtUpCallback,
            silverback => silverback.AddDelegateSubscriber<TestEventWithKafkaKey>(HandleMessage));

        void HandleMessage(TestEventWithKafkaKey message) => Interlocked.Increment(ref processedCount);

        await ProduceAsync(15);

        IKafkaConsumer consumer = await ConnectAndWaitUntilCaughtUpAsync(caughtUpCallback);
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        caughtUpCallback.CallsCount.ShouldBe(1);
        caughtUpCallback.ProcessedCountWhenCaughtUp.ShouldBe(15);

        await ProduceAsync(5);

        await consumer.Client.ConnectAsync();

        await caughtUpCallback.WaitUntilInvokedAsync(2);
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        caughtUpCallback.ProcessedCountWhenCaughtUp.ShouldBe(35);
        processedCount.ShouldBe(35);
    }

    [Fact]
    public async Task CatchUp_ShouldDisconnectOnceCaughtUp_WhenSubscribingWithGroup()
    {
        int processedCount = 0;
        CaughtUpCallback caughtUpCallback = new(() => processedCount);

        await RunHostAsync(
            3,
            consumer => consumer
                .WithGroupId(DefaultGroupId)
                .AutoResetOffsetToEarliest()
                .DisconnectWhenCaughtUp()
                .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)),
            caughtUpCallback,
            silverback => silverback.AddDelegateSubscriber<TestEventWithKafkaKey>(HandleMessage));

        void HandleMessage(TestEventWithKafkaKey message) => Interlocked.Increment(ref processedCount);

        await ProduceAsync(15);

        IKafkaConsumer consumer = await ConnectAndWaitUntilCaughtUpAsync(caughtUpCallback);
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        caughtUpCallback.ProcessedCountWhenCaughtUp.ShouldBe(15);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(15);
    }

    [Fact]
    public async Task CatchUp_ShouldKeepPartitionsPausedAtEnd_WhenReassignedOnceCaughtUp()
    {
        ConcurrentDictionary<string, int> processedCounts = new();
        TaskCompletionSource callbackGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CaughtUpCallback caughtUpCallback = new(() => 0, callbackGate.Task);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(2))
                .ManuallyConnect())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(
                    "consumer1",
                    consumer => consumer
                        .WithGroupId(DefaultGroupId)
                        .AutoResetOffsetToEarliest()
                        .DisconnectWhenCaughtUp()
                        .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                .AddConsumer(
                    "consumer2",
                    consumer => consumer
                        .WithGroupId(DefaultGroupId)
                        .AutoResetOffsetToEarliest()
                        .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddSingletonBrokerClientCallback(caughtUpCallback)
            .AddDelegateSubscriber<IInboundEnvelope<TestEventOne>>(HandleMessage));

        void HandleMessage(IInboundEnvelope<TestEventOne> envelope) =>
            processedCounts.AddOrUpdate(envelope.Consumer.Name, 1, (_, count) => count + 1);

        await ProduceToPartitionAsync(0, 3);
        await ProduceToPartitionAsync(1, 3);

        IKafkaConsumer consumer1 = (IKafkaConsumer)Helper.GetConsumer("consumer1");
        IKafkaConsumer consumer2 = (IKafkaConsumer)Helper.GetConsumer("consumer2");

        try
        {
            await consumer1.Client.ConnectAsync();
            await caughtUpCallback.WaitUntilInvokedAsync(1);

            // While the callback is running, a member joining the group triggers an eager rebalance, which assigns one of the
            // partitions paused at their end back to consumer1
            await consumer2.Client.ConnectAsync();
            await AsyncTestingUtil.WaitAsync(() => consumer1.Client.Assignment.Count == 1 && consumer2.Client.Assignment.Count == 1);

            await ProduceToPartitionAsync(consumer1.Client.Assignment.Single().Partition.Value, 3);
            await Task.Delay(300);

            processedCounts["consumer1"].ShouldBe(6);
        }
        finally
        {
            callbackGate.TrySetResult();
        }

        await AsyncTestingUtil.WaitAsync(() => consumer1.Client.Status == ClientStatus.Disconnected);

        consumer1.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task CatchUp_ShouldNotConsumePastEndOfPartition_WhenDisconnecting()
    {
        ConcurrentDictionary<int, int> processedCounts = new();
        TaskCompletionSource laggingPartitionGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EndOfPartitionCallback endOfPartitionCallback = new();
        CaughtUpCallback caughtUpCallback = new(() => processedCounts.Sum(pair => pair.Value));

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(2))
                .ManuallyConnect())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(
                    "consumer",
                    consumer => consumer
                        .DisconnectWhenCaughtUp()
                        .DisableOffsetsCommit()
                        .Consume<TestEventOne>(endpoint => endpoint.ConsumeFromBeginning(DefaultTopicName))))
            .AddSingletonBrokerClientCallback(caughtUpCallback)
            .AddSingletonBrokerClientCallback(endOfPartitionCallback)
            .AddDelegateSubscriber<IInboundEnvelope<TestEventOne>>(HandleMessageAsync));

        async Task HandleMessageAsync(IInboundEnvelope<TestEventOne> envelope)
        {
            int partition = ((KafkaOffset)envelope.BrokerMessageIdentifier).TopicPartition.Partition.Value;

            if (partition == 1)
                await laggingPartitionGate.Task;

            processedCounts.AddOrUpdate(partition, 1, (_, count) => count + 1);
        }

        await ProduceToPartitionAsync(0, 3);
        await ProduceToPartitionAsync(1, 3);

        IKafkaConsumer consumer = (IKafkaConsumer)Helper.GetConsumer("consumer");
        await consumer.Client.ConnectAsync();

        await AsyncTestingUtil.WaitAsync(() => endOfPartitionCallback.Partitions.Contains(0));

        await ProduceToPartitionAsync(0, 2);
        await Task.Delay(100);

        laggingPartitionGate.SetResult();

        await caughtUpCallback.WaitUntilInvokedAsync(1);
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        processedCounts[0].ShouldBe(3);
        processedCounts[1].ShouldBe(3);
    }

    [Fact]
    public async Task CatchUp_ShouldResumePartitionPausedAtEnd_WhenRestartedBeforeCaughtUp()
    {
        int processedCount = 0;
        TaskCompletionSource firstMessageGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource firstMessageReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CaughtUpCallback caughtUpCallback = new(() => processedCount);

        await RunHostAsync(
            1,
            consumer => consumer
                .DisconnectWhenCaughtUp()
                .DisableOffsetsCommit()
                .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFromBeginning(DefaultTopicName)),
            caughtUpCallback,
            silverback => silverback.AddDelegateSubscriber<TestEventWithKafkaKey>(HandleMessageAsync));

        async Task HandleMessageAsync(TestEventWithKafkaKey message)
        {
            if (firstMessageReceived.TrySetResult())
                await firstMessageGate.Task;

            Interlocked.Increment(ref processedCount);
        }

        await ProduceAsync(5);

        IKafkaConsumer consumer = (IKafkaConsumer)Helper.GetConsumer("consumer");
        await consumer.Client.ConnectAsync();

        // The end of the partition is polled (and the partition paused) while the first message is being processed
        await firstMessageReceived.Task;
        await Task.Delay(200);

        await consumer.StopAsync(waitUntilStopped: false);
        firstMessageGate.SetResult();

        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Stopped);

        await consumer.StartAsync();

        await caughtUpCallback.WaitUntilInvokedAsync(1);
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        caughtUpCallback.ProcessedCountWhenCaughtUp.ShouldBeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task CatchUp_ShouldNotifyOnceAllMessagesAreProcessed_WhenLastMessagesInBatchAreFiltered()
    {
        int processedCount = 0;
        CaughtUpCallback caughtUpCallback = new(() => processedCount);

        await RunHostAsync(
            1,
            consumer => consumer
                .DisconnectWhenCaughtUp()
                .DisableOffsetsCommit()
                .Consume<TestEventOne>(endpoint => endpoint
                    .ConsumeFromBeginning(DefaultTopicName)
                    .EnableBatchProcessing(100)
                    .Filter(message => message?.ContentEventOne != "filtered")),
            caughtUpCallback,
            silverback => silverback.AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IMessageStreamEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne dummy in batch)
            {
                Interlocked.Increment(ref processedCount);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "filtered" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "filtered" });

        IKafkaConsumer consumer = await ConnectAndWaitUntilCaughtUpAsync(caughtUpCallback);
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        caughtUpCallback.ProcessedCountWhenCaughtUp.ShouldBe(2);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CatchUp_ShouldNotifyOnceAllMessagesAreProcessed_WhenFilteredMessageIsFollowedByIncompleteChunkedMessage(
        bool disconnect)
    {
        int processedCount = 0;
        CaughtUpCallback caughtUpCallback = new(() => processedCount);
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(new TestEventOne { ContentEventOne = "incomplete" });

        await RunHostAsync(
            1,
            consumer => (disconnect ? consumer.DisconnectWhenCaughtUp() : consumer.NotifyWhenCaughtUp())
                .DisableOffsetsCommit()
                .Consume<TestEventOne>(endpoint => endpoint
                    .ConsumeFromBeginning(DefaultTopicName)
                    .EnableBatchProcessing(100)
                    .Filter(message => message?.ContentEventOne != "filtered")),
            caughtUpCallback,
            silverback => silverback.AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IMessageStreamEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne dummy in batch)
            {
                Interlocked.Increment(ref processedCount);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "filtered" });
        await producer.RawProduceAsync(
            [.. rawMessage.Take(10)],
            HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            [.. rawMessage.Skip(10).Take(10)],
            HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));

        IKafkaConsumer consumer = await ConnectAndWaitUntilCaughtUpAsync(caughtUpCallback);

        caughtUpCallback.CallsCount.ShouldBe(1);
        caughtUpCallback.ProcessedCountWhenCaughtUp.ShouldBe(1);

        if (disconnect)
        {
            await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);
            consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        }
    }

    [Fact]
    public async Task CatchUp_ShouldKeepCaughtUpPartitionPaused_WhenSharedChannelIsRolledBack()
    {
        ConcurrentDictionary<int, int> processedCounts = new();
        TaskCompletionSource failureGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int failedCount = 0;
        CaughtUpCallback caughtUpCallback = new(() => processedCounts.Sum(pair => pair.Value));

        await RunHostAsync(
            2,
            consumer => consumer
                .DisconnectWhenCaughtUp()
                .DisableOffsetsCommit()
                .ProcessAllPartitionsTogether()
                .Consume<TestEventOne>(endpoint => endpoint
                    .ConsumeFromBeginning(DefaultTopicName)
                    .OnError(policy => policy.Retry(1))),
            caughtUpCallback,
            silverback => silverback.AddDelegateSubscriber<IInboundEnvelope<TestEventOne>>(HandleMessageAsync));

        async Task HandleMessageAsync(IInboundEnvelope<TestEventOne> envelope)
        {
            int partition = ((KafkaOffset)envelope.BrokerMessageIdentifier).TopicPartition.Partition.Value;

            if (partition == 1 && Interlocked.Increment(ref failedCount) == 1)
            {
                await failureGate.Task;
                throw new InvalidOperationException("Test failure");
            }

            processedCounts.AddOrUpdate(partition, 1, (_, count) => count + 1);
        }

        await ProduceToPartitionAsync(0, 1);
        await ProduceToPartitionAsync(1, 1);

        IKafkaConsumer consumer = (IKafkaConsumer)Helper.GetConsumer("consumer");
        await consumer.Client.ConnectAsync();

        // Partition 0 reaches its end and is caught up while the message of partition 1 is being processed
        await AsyncTestingUtil.WaitAsync(() => processedCounts.GetValueOrDefault(0) == 1 && failedCount == 1);
        await Task.Delay(100);

        await ProduceToPartitionAsync(0, 2);
        await Task.Delay(100);

        failureGate.SetResult();

        await caughtUpCallback.WaitUntilInvokedAsync(1);
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        processedCounts[0].ShouldBe(1);
        processedCounts[1].ShouldBe(1);
    }

    [Fact]
    public async Task CatchUp_ShouldNotifyOnceAllEndpointsAreCaughtUp()
    {
        int processedCount = 0;
        CaughtUpCallback caughtUpCallback = new(() => processedCount);

        await RunHostAsync(
            2,
            consumer => consumer
                .NotifyWhenCaughtUp()
                .DisableOffsetsCommit()
                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFromBeginning("topic1"))
                .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFromBeginning("topic2")),
            caughtUpCallback,
            silverback => silverback
                .AddDelegateSubscriber<TestEventOne>(HandleMessageOneAsync)
                .AddDelegateSubscriber<TestEventTwo>(HandleMessageTwo));

        async Task HandleMessageOneAsync(TestEventOne message)
        {
            await Task.Delay(20);
            Interlocked.Increment(ref processedCount);
        }

        void HandleMessageTwo(TestEventTwo message) => Interlocked.Increment(ref processedCount);

        IProducer producer1 = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1")));
        IProducer producer2 = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic2")));

        for (int i = 1; i <= 5; i++)
        {
            await producer1.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await producer2.ProduceAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
        }

        await ConnectAndWaitUntilCaughtUpAsync(caughtUpCallback);

        caughtUpCallback.CallsCount.ShouldBe(1);
        caughtUpCallback.ProcessedCountWhenCaughtUp.ShouldBe(10);
    }

    [Fact]
    public async Task CatchUp_ShouldIgnorePartitionsAssignedOnceCaughtUp()
    {
        CaughtUpCallback caughtUpCallback = new(() => 0);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(2))
                .ManuallyConnect())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(
                    "consumer1",
                    consumer => consumer
                        .WithGroupId(DefaultGroupId)
                        .WithCooperativeStickyPartitionAssignmentStrategy()
                        .NotifyWhenCaughtUp()
                        .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                .AddConsumer(
                    "consumer2",
                    consumer => consumer
                        .WithGroupId(DefaultGroupId)
                        .WithCooperativeStickyPartitionAssignmentStrategy()
                        .NotifyWhenCaughtUp()
                        .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddSingletonBrokerClientCallback(caughtUpCallback)
            .AddDelegateSubscriber<TestEventWithKafkaKey>(HandleMessage));

        static void HandleMessage(TestEventWithKafkaKey message)
        {
            // Consumed only to catch up
        }

        await ProduceAsync(10);

        IKafkaConsumer consumer1 = (IKafkaConsumer)Helper.GetConsumer("consumer1");
        IKafkaConsumer consumer2 = (IKafkaConsumer)Helper.GetConsumer("consumer2");
        await consumer1.Client.ConnectAsync();
        await consumer1.WaitUntilCaughtUpAsync().WaitAsync(CaughtUpTimeout, TestContext.Current.CancellationToken);
        await caughtUpCallback.WaitUntilInvokedAsync(1);

        await consumer2.Client.ConnectAsync();
        await consumer2.WaitUntilCaughtUpAsync().WaitAsync(CaughtUpTimeout, TestContext.Current.CancellationToken);
        await caughtUpCallback.WaitUntilInvokedAsync(2);
        await AsyncTestingUtil.WaitAsync(() => consumer1.Client.Assignment.Count == 1 && consumer2.Client.Assignment.Count == 1);
        await Task.Delay(200);

        caughtUpCallback.CaughtUpConsumers.ShouldBe([consumer1, consumer2]);

        // The partition revoked from consumer1 is assigned back to it
        await consumer2.Client.DisconnectAsync();

        await AsyncTestingUtil.WaitAsync(() => consumer1.Client.Assignment.Count == 2);
        await Task.Delay(200);

        caughtUpCallback.CaughtUpConsumers.ShouldBe([consumer1, consumer2]);
        consumer1.WaitUntilCaughtUpAsync().IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public async Task WaitUntilCaughtUpAsync_ShouldThrow_WhenCatchUpIsNotEnabled()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(
                    "consumer",
                    consumer => consumer
                        .WithGroupId(DefaultGroupId)
                        .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))));

        IKafkaConsumer consumer = (IKafkaConsumer)Helper.GetConsumer("consumer");

        Func<Task> act = () => consumer.WaitUntilCaughtUpAsync();

        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    private async Task RunHostAsync(
        int partitionsCount,
        Action<KafkaConsumerConfigurationBuilder> configureConsumer,
        CaughtUpCallback caughtUpCallback,
        Action<SilverbackBuilder>? configureSubscribers = null) =>
        await Host.ConfigureServicesAndRunAsync(services =>
        {
            SilverbackBuilder silverbackBuilder = services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(partitionsCount))
                    .ManuallyConnect())
                .AddKafkaClients(clients => clients
                    .WithBootstrapServers("PLAINTEXT://e2e")
                    .AddConsumer("consumer", configureConsumer))
                .AddSingletonBrokerClientCallback(caughtUpCallback);

            configureSubscribers?.Invoke(silverbackBuilder);
        });

    private async Task<IKafkaConsumer> ConnectAndWaitUntilCaughtUpAsync(CaughtUpCallback caughtUpCallback)
    {
        IKafkaConsumer consumer = (IKafkaConsumer)Helper.GetConsumer("consumer");
        await consumer.Client.ConnectAsync();

        await consumer.WaitUntilCaughtUpAsync().WaitAsync(CaughtUpTimeout, TestContext.Current.CancellationToken);
        await caughtUpCallback.WaitUntilInvokedAsync(1);

        return consumer;
    }

    private async Task ProduceAsync(int count)
    {
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= count; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i, Content = $"{i}" });
        }
    }

    private async Task ProduceToPartitionAsync(int partition, int count)
    {
        IProducer producer = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName, partition)));

        for (int i = 1; i <= count; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }
    }

    private sealed class CaughtUpCallback : IKafkaConsumerCaughtUpCallback
    {
        private readonly Func<int> _getProcessedCount;

        private readonly ConcurrentQueue<IKafkaConsumer> _caughtUpConsumers = new();

        private readonly Task? _gate;

        public CaughtUpCallback(Func<int> getProcessedCount, Task? gate = null)
        {
            _getProcessedCount = getProcessedCount;
            _gate = gate;
        }

        public int CallsCount => _caughtUpConsumers.Count;

        public IReadOnlyCollection<IKafkaConsumer> CaughtUpConsumers => [.. _caughtUpConsumers];

        public int ProcessedCountWhenCaughtUp { get; private set; } = -1;

        public async Task OnConsumerCaughtUpAsync(IKafkaConsumer consumer)
        {
            ProcessedCountWhenCaughtUp = _getProcessedCount();
            _caughtUpConsumers.Enqueue(consumer);

            if (_gate != null)
                await _gate;
        }

        public Task WaitUntilInvokedAsync(int count) => AsyncTestingUtil.WaitAsync(() => CallsCount >= count, CaughtUpTimeout);
    }

    private sealed class EndOfPartitionCallback : IKafkaPartitionEofCallback
    {
        private readonly ConcurrentBag<int> _partitions = [];

        public IReadOnlyCollection<int> Partitions => [.. _partitions];

        public void OnEndOfTopicPartitionReached(TopicPartition topicPartition, IKafkaConsumer consumer) =>
            _partitions.Add(topicPartition.Partition.Value);
    }
}
