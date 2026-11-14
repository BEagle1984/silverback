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
