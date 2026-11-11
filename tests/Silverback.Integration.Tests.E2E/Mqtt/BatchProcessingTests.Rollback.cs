// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
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
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class BatchProcessingTests
{
    [Fact]
    public async Task Batch_ShouldReplayWholeBatch_WhenFirstMessageIsUnhandledAndRetryPolicyIsApplied()
    {
        TestingCollection<List<string?>> receivedBatches = [];
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5))
                        .IgnoreUnhandledMessages()
                        .OnError(policy => policy.Retry(2)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);

                // Fail the first attempt in the middle of the first batch
                if (message.ContentEventOne == "2" && Interlocked.Increment(ref tryCount) == 1)
                    throw new InvalidOperationException("Retry!");
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        // The unhandled message starts the first batch without being added to it
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "unhandled" });

        for (int i = 1; i <= 6; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(3);
        receivedBatches[0].ShouldBe(["1", "2"]);
        receivedBatches[1].ShouldBe(["1", "2", "3"]);
        receivedBatches[2].ShouldBe(["4", "5", "6"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldReplayBatchFromFirstMessage_WhenFirstMessageIsUnhandledAndConsumerIsStoppedAndRestarted()
    {
        TestingCollection<List<string?>> receivedBatches = [];
        TaskCompletionSource<bool> secondMessageReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5))
                        .IgnoreUnhandledMessages())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);

                if (list.Count == 2)
                    secondMessageReceived.TrySetResult(true);
            }
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        // The unhandled message starts the first batch without being added to it
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "unhandled" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await secondMessageReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // The unhandled message isn't acknowledged while the batch it started is pending
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(3);

        await consumer.StopAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(3);

        for (int i = 3; i <= 6; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await consumer.StartAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(3);
        receivedBatches[0].ShouldBe(["1", "2"]);
        receivedBatches[1].ShouldBe(["1", "2", "3"]);
        receivedBatches[2].ShouldBe(["4", "5", "6"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldReplayBatchAndNextMessagesInOrder_WhenBatchCompletedByTimeoutFailsWhileNextMessagesAreRead()
    {
        TestingCollection<List<string?>> receivedBatches = [];
        TestingCollection<List<string?>> successfulBatches = [];
        TaskCompletionSource<bool> firstBatchCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> nextMessagesRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(10, TimeSpan.FromSeconds(1))
                        .OnError(policy => policy.Retry(2)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
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
                // The batch started by the next messages is aborted to replay the failed batch
                return;
            }

            // Fail the first batch, completed by the timeout, while the reader is already processing the next messages
            if (Interlocked.Increment(ref completedBatches) == 1)
            {
                firstBatchCompleted.TrySetResult(true);
                await nextMessagesRead.Task.WaitAsync(TimeSpan.FromSeconds(30));
                throw new InvalidOperationException("Retry!");
            }

            successfulBatches.Add(list);
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await firstBatchCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        for (int i = 4; i <= 5; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        // The reader moves on to the next message while the completed batch is still being processed
        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.ConsumedMessagesCount >= 4);
        consumer.StatusInfo.ConsumedMessagesCount.ShouldBeGreaterThanOrEqualTo(4);

        nextMessagesRead.TrySetResult(true);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches[0].ShouldBe(["1", "2", "3"]);
        receivedBatches[^1].ShouldBe(["1", "2", "3", "4", "5"]);
        successfulBatches.ShouldHaveSingleItem().ShouldBe(["1", "2", "3", "4", "5"]);

        // The batch started by the next messages before the replay (if any) is aborted and receives only them, in order
        foreach (List<string?> abortedBatch in receivedBatches.Skip(1).SkipLast(1))
        {
            abortedBatch.ShouldBe(new[] { "4", "5" }.Take(abortedBatch.Count));
        }

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldStartNextBatchOnlyAfterFailedBatchIsSkipped()
    {
        TestingCollection<List<string?>> receivedBatches = [];
        TaskCompletionSource<bool> skipping = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseSkip = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int completedBatches = 0;

        // Delays the commit of the skipped batch, while the next message is consumed
        ISkipMessageErrorPolicyCallback callback = Substitute.For<ISkipMessageErrorPolicyCallback>();
        callback.OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                skipping.TrySetResult(true);
                return releaseSkip.Task.WaitAsync(TimeSpan.FromSeconds(30));
            });

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(10, TimeSpan.FromSeconds(1))
                        .OnError(policy => policy.Retry(1, retry => retry.ApplyTo<TimeoutException>()).ThenSkip()))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(callback));

        // The batches are completed by the timeout: the first one is skipped, the second one is retried
        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);
            }

            switch (Interlocked.Increment(ref completedBatches))
            {
                case 1:
                    throw new InvalidOperationException("Skip!");
                case 2:
                    throw new TimeoutException("Retry!");
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await skipping.Task.WaitAsync(TimeSpan.FromSeconds(10));

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        // The next batch isn't started while the failed one is being skipped
        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Count > 1, TimeSpan.FromSeconds(2));
        receivedBatches.Count.ShouldBe(1);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(3);

        // Then the next batch is retried, without replaying the skipped one
        releaseSkip.TrySetResult(true);
        await AsyncTestingUtil.WaitAsync(() => completedBatches == 3);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Select(batch => batch.ToArray()).ShouldBe([["1", "2"], ["3"], ["3"]]);
        callback.ReceivedCalls().Count().ShouldBe(1);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
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
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(10, TimeSpan.FromSeconds(1))
                        .OnError(policy => policy.Retry(1)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(callback));

        // The first batch is completed by the timeout and fails once
        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
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
                // The batch started by the next message when the reader restarts is aborted
                return;
            }

            if (Interlocked.Increment(ref completedBatches) == 1)
                throw new InvalidOperationException("Retry!");

            successfulBatches.Add(list);
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await retrying.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // The next batch isn't started while the error policies are applied to the failed one
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });
        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Count > 1, TimeSpan.FromSeconds(2));
        receivedBatches.Count.ShouldBe(1);

        releaseRetry.TrySetResult(true);

        // The failed batch is replayed, followed by the next message
        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Sum(batch => batch.Count) >= 3);
        successfulBatches.SelectMany(batch => batch).ShouldBe(["1", "2", "3"]);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldNotAcknowledgeNextBatch_WhileFailedBatchCompletedByTimeoutIsHandled()
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
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(2, TimeSpan.FromSeconds(1)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(callback));

        // The first batch is completed by the timeout and fails, stopping the consumer
        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
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

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await stopping.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // The next messages would complete the next batch, which must not be acknowledged before the failed one is handled
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });
        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Count > 0, TimeSpan.FromSeconds(2));

        successfulBatches.ShouldBeEmpty();
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(3);

        // The stop policy then stops the consumer before the next batch is completed
        releaseStop.TrySetResult(true);
        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Count > 0, TimeSpan.FromSeconds(2));

        successfulBatches.ShouldBeEmpty();
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(3);
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
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5))
                        .IgnoreUnhandledMessages()
                        .OnError(policy => policy.Retry(2).ThenSkip()))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(callback));

        static async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne dummy in batch)
            {
                // Just consume the batch
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        // The unhandled message starts the batch without being added to it, then a message fails before the sequencer
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "unhandled" });
        await producer.RawProduceAsync(
            "{ invalid"u8.ToArray(),
            [new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)]);

        await AsyncTestingUtil.WaitAsync(() => skipsCount > 0);

        retriesCount.ShouldBe(2);
        skipsCount.ShouldBe(1);

        // The batch started by the ignored message is completed and every delivery is acknowledged
        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldReplayPendingBatchesInOrder_WhenConsumerIsStoppedAndRestartedWhileProcessingInParallel()
    {
        TestingCollection<List<int>> receivedBatches = [];
        TestingCollection<List<int>> completedBatches = [];
        int receivedMessagesCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .EnableParallelProcessing(3)
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(4, TimeSpan.FromMinutes(5)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<int> list = [];
            receivedBatches.Add(list);

            try
            {
                await foreach (TestEventOne message in batch)
                {
                    list.Add(int.Parse(message.ContentEventOne!, CultureInfo.InvariantCulture));
                    Interlocked.Increment(ref receivedMessagesCount);
                }
            }
            catch (OperationCanceledException)
            {
                // The pending batches are aborted when the consumer is stopped
                return;
            }

            completedBatches.Add(list);
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        // The messages are spread round-robin across the channels: each pending batch gets 2 messages
        for (int i = 1; i <= 6; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessagesCount == 6);
        receivedMessagesCount.ShouldBe(6);

        await consumer.StopAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(6);

        // Each pending batch is replayed, followed by the message received while stopped
        for (int i = 7; i <= 9; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await consumer.StartAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        await AsyncTestingUtil.WaitAsync(() => receivedMessagesCount == 6 + 9);
        receivedMessagesCount.ShouldBe(6 + 9);

        await consumer.StopAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(9);

        // Each pending batch is replayed again and completed by the message received while stopped
        for (int i = 10; i <= 12; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await consumer.StartAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessagesCount.ShouldBe(6 + 9 + 12);
        completedBatches.Select(batch => batch.ToArray()).OrderBy(batch => batch[0])
            .ShouldBe([[1, 4, 7, 10], [2, 5, 8, 11], [3, 6, 9, 12]]);

        // Each batch is replayed from its first message, in order
        receivedBatches.Where(batch => batch[0] == 1).Select(batch => batch.ToArray()).ShouldBe([[1, 4], [1, 4, 7], [1, 4, 7, 10]]);
        receivedBatches.Where(batch => batch[0] == 2).Select(batch => batch.ToArray()).ShouldBe([[2, 5], [2, 5, 8], [2, 5, 8, 11]]);
        receivedBatches.Where(batch => batch[0] == 3).Select(batch => batch.ToArray()).ShouldBe([[3, 6], [3, 6, 9], [3, 6, 9, 12]]);
        receivedBatches.Count.ShouldBe(9);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }
}
