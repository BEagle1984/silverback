// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(OperationCanceledException))]
    [InlineData(typeof(TaskCanceledException))] // E.g., an HttpClient timeout, which isn't caused by the consumer stop
    public async Task Batch_ShouldStopConsumer_WhenProcessingFailsWhileOtherSubscriberIsEnumerating(Type exceptionType)
    {
        List<string?> receivedMessages = [];
        List<string?> otherReceivedMessages = [];
        IStopConsumerErrorPolicyCallback callback = Substitute.For<IStopConsumerErrorPolicyCallback>();

        // The other subscriber is registered first: the test fails if the cancellation of its stream, aborted because of the failure, is
        // rethrown instead of the exception of the failed subscriber
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
                        .EnableBatchProcessing(6))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventTwo>>(HandleOtherBatch)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch)
            .AddSingletonErrorPolicyCallback(callback));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne message in batch)
            {
                receivedMessages.Add(message.ContentEventOne);

                if (message.ContentEventOne == "2")
                    throw (Exception)Activator.CreateInstance(exceptionType, "Test")!;
            }
        }

        async ValueTask HandleOtherBatch(IAsyncEnumerable<TestEventTwo> batch)
        {
            await foreach (TestEventTwo message in batch)
            {
                otherReceivedMessages.Add(message.ContentEventTwo);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await producer.ProduceAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
        }

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Stopped);

        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        receivedMessages.ShouldBe(["1", "2"]);
        otherReceivedMessages.ShouldBe(["1"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        await callback.Received(1).OnStoppingConsumerAsync(
            Arg.Any<IRawInboundEnvelope>(),
            Arg.Is<Exception>(exception => exception.GetType() == exceptionType && exception.Message == "Test"));
        callback.ReceivedCalls().Count().ShouldBe(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Batch_ShouldRollBackWithoutApplyingErrorPolicy_WhenStoppingWhileSeveralSubscribersAreEnumerating(
        bool isProcessingWhenStopping)
    {
        int processingCount = 0;
        int otherProcessingCount = 0;
        IStopConsumerErrorPolicyCallback callback = Substitute.For<IStopConsumerErrorPolicyCallback>();

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
                        .EnableBatchProcessing(6))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventTwo>>(HandleOtherBatch)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, CancellationToken>(HandleBatch)
            .AddSingletonErrorPolicyCallback(callback));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch, CancellationToken cancellationToken)
        {
            await foreach (TestEventOne dummy in batch)
            {
                Interlocked.Increment(ref processingCount);

                // Canceled by the stop, otherwise the batch is aborted by the stop while waiting for the next message
                if (isProcessingWhenStopping)
                    await Task.Delay(Timeout.Infinite, cancellationToken);
            }
        }

        async ValueTask HandleOtherBatch(IAsyncEnumerable<TestEventTwo> batch)
        {
            await foreach (TestEventTwo dummy in batch)
            {
                // Ignore the cancellation token
                Interlocked.Increment(ref otherProcessingCount);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventTwo { ContentEventTwo = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await AsyncTestingUtil.WaitAsync(() => processingCount == 1);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.StopAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
        otherProcessingCount.ShouldBe(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        callback.ReceivedCalls().ShouldBeEmpty();

        // The batch is rolled back and consumed again
        await consumer.StartAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        await AsyncTestingUtil.WaitAsync(() => processingCount == 2 && otherProcessingCount == 2);

        processingCount.ShouldBe(2);
        otherProcessingCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Batch_ShouldStopConsumerAndDisconnect_WhenBatchCompletedByTimeoutFails(bool isErrorHandlingFailed)
    {
        int stopCalls = 0;

        // When the error handling fails (the stop callback throws), the rollback doesn't stop the consumer
        IStopConsumerErrorPolicyCallback callback = Substitute.For<IStopConsumerErrorPolicyCallback>();
        callback.OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref stopCalls);
                return isErrorHandlingFailed ? Task.FromException(new InvalidOperationException("Callback failed.")) : Task.CompletedTask;
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
                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(300))
                        .OnError(policy => policy.Stop()))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(callback));

        // No message is being added to the batch when the subscriber fails, after enumerating the whole batch completed by its max wait
        // time, so no reader rethrows the exception
        static async ValueTask HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne dummy in batch)
            {
                // Enumerate the whole batch before failing
            }

            throw new InvalidOperationException("Test");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Message 1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Message 2" });

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected, TimeSpan.FromSeconds(10));

        stopCalls.ShouldBe(1);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldNotDisconnect_WhenBatchCompletedByTimeoutFailsAfterConsumerIsRestarted()
    {
        TestingCollection<List<string?>> successfulBatches = [];
        TaskCompletionSource<bool> firstBatchEnumerated = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> failFirstBatch = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int batchesCount = 0;

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
                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(300))
                        .OnError(policy => policy.Stop()))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        // The first batch, completed by its max wait time, keeps being processed while the consumer is restarted (ignoring the
        // cancellation), and only then fails
        async ValueTask HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);
            }

            if (Interlocked.Increment(ref batchesCount) == 1)
            {
                firstBatchEnumerated.TrySetResult(true);
                await failFirstBatch.Task.WaitAsync(TimeSpan.FromSeconds(30));
                throw new InvalidOperationException("Test");
            }

            successfulBatches.Add(list);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        await firstBatchEnumerated.Task.WaitAsync(TimeSpan.FromSeconds(30));

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.StopAsync(false);
        await consumer.StartAsync();
        failFirstBatch.TrySetResult(true);

        // The stop requested by the stale error handling is ignored and the restarted consumer processes the messages again
        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Count > 0, TimeSpan.FromSeconds(10));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        successfulBatches.ShouldHaveSingleItem().ShouldBe(["1", "2"]);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(2);
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
        await AsyncTestingUtil.WaitAsync(() => Helper.GetConsumerForEndpoint(DefaultTopicName).Client.Status == ClientStatus.Disconnected);
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
    public async Task Batch_ShouldReplayBatchCompletedByTimeout_WhenItFailsWhileNextMessageIsRolledBack()
    {
        TestingCollection<List<string?>> successfulBatches = [];
        TaskCompletionSource<bool> firstBatchCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseFirstBatch = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> nextMessageRetrying = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int completedBatches = 0;

        IRetryErrorPolicyCallback callback = Substitute.For<IRetryErrorPolicyCallback>();
        callback.OnRetryingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(call =>
            {
                if (call.ArgAt<Exception>(1) is JsonException)
                    nextMessageRetrying.TrySetResult(true);

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
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(500))
                        .OnError(policy => policy.Retry(1).ThenSkip()))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch)
            .AddSingletonErrorPolicyCallback(callback));

        // The first batch is completed by the timeout and fails once, after the next message started rolling back the partition
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
                return;
            }

            if (Interlocked.Increment(ref completedBatches) == 1)
            {
                firstBatchCompleted.TrySetResult(true);
                await releaseFirstBatch.Task.WaitAsync(TimeSpan.FromSeconds(30));
                throw new InvalidOperationException("Retry!");
            }

            successfulBatches.Add(list);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await firstBatchCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // The next message fails before the sequencer and is rolled back while the first batch is still being processed
        await producer.RawProduceAsync(
            "{ invalid"u8.ToArray(),
            [new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)]);
        await nextMessageRetrying.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await Task.Delay(TimeSpan.FromMilliseconds(500)); // Let the rollback of the next message start first, as in the reported issue

        releaseFirstBatch.TrySetResult(true);

        // The first batch is consumed again, although its rollback overlapped the pending one
        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Count > 0);
        successfulBatches.SelectMany(batch => batch).ShouldBe(["1"]);

        // The skipped message is committed with the next one
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Count > 1);
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        successfulBatches.SelectMany(batch => batch).ShouldBe(["1", "2"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);
    }

    [Fact]
    public async Task Batch_ShouldNotCommitSkippedMessage_WhileBatchCompletedByTimeoutIsUnresolved()
    {
        TaskCompletionSource<bool> firstBatchCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseFirstBatch = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> nextMessageSkipped = new(TaskCreationOptions.RunContinuationsAsynchronously);

        ISkipMessageErrorPolicyCallback callback = Substitute.For<ISkipMessageErrorPolicyCallback>();
        callback.OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                nextMessageSkipped.TrySetResult(true);
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
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(500))
                        .OnError(policy => policy.Skip(skip => skip.ApplyTo<JsonException>())))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch)
            .AddSingletonErrorPolicyCallback(callback));

        // The first batch is completed by the timeout and then fails, stopping the consumer
        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne dummy in batch)
            {
                // Just consume the batch
            }

            firstBatchCompleted.TrySetResult(true);
            await releaseFirstBatch.Task.WaitAsync(TimeSpan.FromSeconds(30));
            throw new InvalidOperationException("Stop!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await firstBatchCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // The next message is skipped while the first batch is still being processed: committing it would commit the batch as well
        await producer.RawProduceAsync(
            "{ invalid"u8.ToArray(),
            [new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)]);
        await nextMessageSkipped.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await AsyncTestingUtil.WaitAsync(() => DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) > 0, TimeSpan.FromSeconds(1));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        // The first batch then fails and stops the consumer, without committing anything
        releaseFirstBatch.TrySetResult(true);
        await AsyncTestingUtil.WaitAsync(() => DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) > 0, TimeSpan.FromSeconds(2));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldProcessNextMessageOnce_WhenFailedBatchIsRolledBackWhileNextMessageWaits()
    {
        TestingCollection<string?> processedMessages = [];
        TaskCompletionSource<bool> firstBatchCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseFirstBatch = new(TaskCreationOptions.RunContinuationsAsynchronously);
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
                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(500))
                        .OnError(policy => policy.Retry(1)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch)
            .AddIntegrationSpy());

        // The first batch is completed by the timeout and fails once, while the next message waits for it
        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            try
            {
                await foreach (TestEventOne message in batch)
                {
                    processedMessages.Add(message.ContentEventOne);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (Interlocked.Increment(ref completedBatches) == 1)
            {
                firstBatchCompleted.TrySetResult(true);
                await releaseFirstBatch.Task.WaitAsync(TimeSpan.FromSeconds(30));
                throw new InvalidOperationException("Retry!");
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await firstBatchCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.RawInboundEnvelopes.Count >= 2);
        await Task.Delay(TimeSpan.FromMilliseconds(200)); // Let the next message wait for the first batch, to start the next one

        // The rollback cancels the reading: the next message doesn't start a batch, which would be aborted (and the message processed
        // again after the seek)
        releaseFirstBatch.TrySetResult(true);
        await AsyncTestingUtil.WaitAsync(() => completedBatches > 1);
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        processedMessages.ShouldBe(["1", "1", "2"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(2);
    }

    [Fact]
    public async Task Batch_ShouldNotCommitSkippedMessage_WhenStoppingWhileBatchIsPending()
    {
        TestingCollection<List<string?>> successfulBatches = [];
        TaskCompletionSource<bool> messageSkipping = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> batchCanceled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int skipsCount = 0;

        ISkipMessageErrorPolicyCallback callback = Substitute.For<ISkipMessageErrorPolicyCallback>();
        callback.OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(async _ =>
            {
                if (Interlocked.Increment(ref skipsCount) > 1)
                    return;

                // Skip the message only after the stop aborted the pending batch, removing it from the store
                messageSkipping.TrySetResult(true);
                await batchCanceled.Task.WaitAsync(TimeSpan.FromSeconds(10));
                await Task.Delay(TimeSpan.FromMilliseconds(200));
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
                        .OnError(policy => policy.Skip(skip => skip.ApplyTo<JsonException>())))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, CancellationToken>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(callback));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch, CancellationToken cancellationToken)
        {
            List<string?> list = [];

            try
            {
                await foreach (TestEventOne message in batch.WithCancellation(cancellationToken))
                {
                    list.Add(message.ContentEventOne);
                }
            }
            catch (OperationCanceledException)
            {
                batchCanceled.TrySetResult(true);
                throw;
            }

            successfulBatches.Add(list);
        }

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await producer.RawProduceAsync(
            "{ invalid"u8.ToArray(),
            [new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)]);
        await messageSkipping.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // The stop aborts the pending batch without rolling it back: committing the skipped message would commit the batch as well
        await consumer.StopAsync();

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        // The batch is consumed again after the restart, followed by the skipped message
        await consumer.StartAsync();
        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Count > 0);
        await AsyncTestingUtil.WaitAsync(() => DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) >= 3);

        successfulBatches.ShouldHaveSingleItem().ShouldBe(["1", "2", "3"]);
        skipsCount.ShouldBe(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);
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
