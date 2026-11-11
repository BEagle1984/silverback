// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class BatchProcessingTests
{
    [Fact]
    public async Task Batch_ShouldStopConsumer_WhenProcessingFails()
    {
        int receivedMessages = 0;

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

        receivedMessages.ShouldBe(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
    }

    [Fact]
    public async Task Batch_ShouldStopConsumer_WhenProcessingFailsWithMultiplePendingBatches()
    {
        int batchesCount = 0;
        int abortedCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventWithKafkaKey>(endpoint => endpoint
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

        batchesCount.ShouldBeGreaterThan(1);
        abortedCount.ShouldBeGreaterThanOrEqualTo(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
    }

    [Fact]
    public async Task Batch_ShouldStopConsumer_WhenProcessingFailsWithIncompatibleSubscriber()
    {
        int batchesCount = 0;
        int abortedCount = 0;

        CountdownEvent batchesStartedCountDown = new(3);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventWithKafkaKey>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(10))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventWithKafkaKey>>(HandleBatch)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventTwo>>(HandleIncompatibleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventWithKafkaKey> eventsStream)
        {
            int batchIndex = Interlocked.Increment(ref batchesCount);

            int messagesCount = 0;

            batchesStartedCountDown.Signal();

            try
            {
                await foreach (TestEventWithKafkaKey dummy in eventsStream)
                {
                    if (batchIndex == 2 && ++messagesCount == 2)
                    {
                        // Ensure that all batches have started before throwing the exception and cause the abort
                        batchesStartedCountDown.Wait();

                        throw new InvalidOperationException("Test");
                    }
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

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.Status == ConsumerStatus.Stopped);
        await AsyncTestingUtil.WaitAsync(() => abortedCount == 2);

        batchesCount.ShouldBe(3);
        abortedCount.ShouldBe(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        Helper.GetConsumerForEndpoint(DefaultTopicName).Client.Status.ShouldBe(ClientStatus.Disconnected);
        Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
    }

    [Fact]
    public async Task Batch_ShouldStartNextBatchOnlyAfterFailedBatchIsRolledBack()
    {
        TestingCollection<List<string?>> receivedBatches = [];
        TestingCollection<List<string?>> successfulBatches = [];
        TaskCompletionSource<bool> retrying = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseRetry = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int completedBatches = 0;

        // Delays the rollback of the failed batch, while the next message is consumed
        IRetryErrorPolicyCallback callback = Substitute.For<IRetryErrorPolicyCallback>();
        callback.OnRetryingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                retrying.TrySetResult(true);
                return releaseRetry.Task.WaitAsync(TimeSpan.FromSeconds(30));
            });

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
                        .EnableBatchProcessing(10, TimeSpan.FromSeconds(1))
                        .OnError(policy => policy.Retry(1)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch)
            .AddSingletonErrorPolicyCallback(callback));

        // The first batch is completed by the timeout and fails once
        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            try
            {
                await foreach (TestEventOne message in batch)
                {
                    list.Add(message.ContentEventOne);
                }
            }
            catch (OperationCanceledException)
            {
                // The batch started by the next message when the partition is rolled back is aborted
                return;
            }

            if (Interlocked.Increment(ref completedBatches) == 1)
                throw new InvalidOperationException("Retry!");

            successfulBatches.Add(list);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await retrying.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // The next batch isn't started while the error policies are applied to the failed one
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Count > 1, TimeSpan.FromSeconds(2));
        receivedBatches.Count.ShouldBe(1);

        releaseRetry.TrySetResult(true);

        // The failed batch is consumed again, followed by the next message
        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Sum(batch => batch.Count) >= 3);
        successfulBatches.SelectMany(batch => batch).ShouldBe(["1", "2", "3"]);
        await Helper.WaitUntilAllMessagesAreCommittedAsync();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);
    }

    [Fact]
    public async Task Batch_ShouldNotCommitNextBatch_WhileFailedBatchCompletedByTimeoutIsHandled()
    {
        TestingCollection<List<string?>> successfulBatches = [];
        TaskCompletionSource<bool> stopping = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseStop = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int completedBatches = 0;

        // Delays the error handling of the failed batch, while the next messages are consumed
        IStopConsumerErrorPolicyCallback callback = Substitute.For<IStopConsumerErrorPolicyCallback>();
        callback.OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                stopping.TrySetResult(true);
                return releaseStop.Task.WaitAsync(TimeSpan.FromSeconds(30));
            });

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
                        .EnableBatchProcessing(2, TimeSpan.FromSeconds(1)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch)
            .AddSingletonErrorPolicyCallback(callback));

        // The first batch is completed by the timeout and fails, stopping the consumer
        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];

            try
            {
                await foreach (TestEventOne message in batch)
                {
                    list.Add(message.ContentEventOne);
                }
            }
            catch (OperationCanceledException)
            {
                // The batch started after the failed one is aborted by the stop
                return;
            }

            if (Interlocked.Increment(ref completedBatches) == 1)
                throw new InvalidOperationException("Stop!");

            successfulBatches.Add(list);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await stopping.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // The next messages would complete the next batch, committing the failed one implicitly
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Count > 0, TimeSpan.FromSeconds(2));

        successfulBatches.ShouldBeEmpty();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        // The stop policy then stops the consumer before the next batch is completed
        releaseStop.TrySetResult(true);
        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Count > 0, TimeSpan.FromSeconds(2));

        successfulBatches.ShouldBeEmpty();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldRetryMessage_WhenDeserializationFailsWhileBatchWithIgnoredMessagesIsPending()
    {
        int retriesCount = 0;
        int skipsCount = 0;
        IErrorPolicyCallback callback = Substitute.For<IRetryErrorPolicyCallback, ISkipMessageErrorPolicyCallback>();
        ((IRetryErrorPolicyCallback)callback).OnRetryingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref retriesCount);
                return Task.CompletedTask;
            });
        ((ISkipMessageErrorPolicyCallback)callback).OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref skipsCount);
                return Task.CompletedTask;
            });

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
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5))
                        .IgnoreUnhandledMessages()
                        .OnError(policy => policy.Retry(2).ThenSkip()))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch)
            .AddSingletonErrorPolicyCallback(callback));

        static async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne dummy in batch)
            {
                // Just consume the batch
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        // The unhandled message starts the batch without being added to it, then a message fails before the sequencer
        await producer.ProduceAsync(new TestEventTwo { ContentEventTwo = "unhandled" });
        await producer.RawProduceAsync(
            "{ invalid"u8.ToArray(),
            [new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)]);

        await AsyncTestingUtil.WaitAsync(() => skipsCount > 0);

        retriesCount.ShouldBe(2);
        skipsCount.ShouldBe(1);

        // The batch started by the ignored message is completed and every message is committed
        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);
    }
}
