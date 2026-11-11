// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class BatchProcessingTests : MqttTests
{
    public BatchProcessingTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Batch_ShouldConsumeAndAcknowledgeInBatch()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
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
                        .EnableBatchProcessing(10, TimeSpan.FromMinutes(5)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15 && completedBatches == 1);

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(5);
        completedBatches.ShouldBe(1);

        // The messages of the incomplete batch aren't acknowledged yet
        await AsyncTestingUtil.WaitAsync(() => DefaultClientSession.GetPendingMessagesCount() == 5);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(5);

        for (int i = 16; i <= 20; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches.SelectMany(batch => batch).Select(message => message.ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 20).Select(i => $"{i}"));
        completedBatches.ShouldBe(2);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldCompletePendingBatchAfterTimeout()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
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
                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(200)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => completedBatches == 2);

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(5);
        await AsyncTestingUtil.WaitAsync(() => DefaultClientSession.GetPendingMessagesCount() == 0);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldCreateConcurrentBatches_WhenProcessingInParallel()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        TaskCompletionSource<bool> allBatchesStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int startedBatches = 0;

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
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            if (Interlocked.Increment(ref startedBatches) == 3)
                allBatchesStarted.TrySetResult(true);

            // The batches are processed concurrently: none completes before all are started
            await allBatchesStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

            await foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 12; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(3);
        receivedBatches.ShouldAllBe(batch => batch.Count == 4);

        // The messages are spread round-robin across the parallel batches
        receivedBatches.Select(batch => batch.Select(message => int.Parse(message.ContentEventOne!, CultureInfo.InvariantCulture)).ToArray())
            .OrderBy(batch => batch[0])
            .ShouldBe([[1, 4, 7, 10], [2, 5, 8, 11], [3, 6, 9, 12]]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldReplayBatchInOrder_WhenRetryPolicyIsApplied()
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
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(5, TimeSpan.FromMinutes(5))
                        .OnError(policy => policy.Retry(2)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);

                // Fail the first two attempts in the middle of the first batch
                if (message.ContentEventOne == "3" && Interlocked.Increment(ref tryCount) <= 2)
                    throw new InvalidOperationException("Retry!");
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 10; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(4);
        receivedBatches[0].ShouldBe(["1", "2", "3"]);
        receivedBatches[1].ShouldBe(["1", "2", "3"]);
        receivedBatches[2].ShouldBe(["1", "2", "3", "4", "5"]);
        receivedBatches[3].ShouldBe(["6", "7", "8", "9", "10"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldReplayOnlyFailedBatch_WhenRetryPolicyIsAppliedWhileProcessingInParallel()
    {
        TestingCollection<List<int>> receivedBatches = [];
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .EnableParallelProcessing(2)
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5))
                        .OnError(policy => policy.Retry(1)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<int> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(int.Parse(message.ContentEventOne!, CultureInfo.InvariantCulture));

                // Fail the first attempt of the batch of the second channel
                if (message.ContentEventOne == "4" && Interlocked.Increment(ref tryCount) == 1)
                    throw new InvalidOperationException("Retry!");
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 6; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Where(batch => batch[0] == 1).ShouldHaveSingleItem().ShouldBe([1, 3, 5]);
        receivedBatches.Where(batch => batch[0] == 2).Select(batch => batch.ToArray()).ShouldBe([[2, 4], [2, 4, 6]]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldSkipBatch_WhenSkipPolicyIsApplied()
    {
        TestingCollection<List<string?>> receivedBatches = [];

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
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5))
                        .OnError(policy => policy.Skip()))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);

                if (message.ContentEventOne == "3")
                    throw new InvalidOperationException("Skip!");
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 6; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].ShouldBe(["1", "2", "3"]);
        receivedBatches[1].ShouldBe(["4", "5", "6"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldStopConsumerWithoutAcknowledging_WhenProcessingFails()
    {
        int receivedMessages = 0;

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
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne unused in batch)
            {
                if (Interlocked.Increment(ref receivedMessages) == 2)
                    throw new InvalidOperationException("Test");
            }
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        receivedMessages.ShouldBe(2);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(3);
    }

    [Fact]
    public async Task Batch_ShouldReplayBatchFromFirstMessage_WhenConsumerIsStoppedAndRestarted()
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
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5)))))
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

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await secondMessageReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));

        await consumer.StopAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(2);

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });
        await consumer.StartAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].ShouldBe(["1", "2"]);
        receivedBatches[1].ShouldBe(["1", "2", "3"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldAcknowledgeSkippedMessage_WhenBatchIsPending()
    {
        TestingCollection<List<string?>> receivedBatches = [];

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
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5))
                        .OnError(policy => policy.Skip()))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });

        // Not deserializable: skipped while the batch is pending
        await producer.RawProduceAsync("not json"u8.ToArray());

        await AsyncTestingUtil.WaitAsync(() => DefaultClientSession.GetPendingMessagesCount() == 1);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(1);

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.ShouldHaveSingleItem().ShouldBe(["1", "2", "3"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldAcknowledgeIgnoredUnhandledMessages()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

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
                        .EnableBatchProcessing(2, TimeSpan.FromMinutes(5))
                        .IgnoreUnhandledMessages())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne());
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "Unhandled message" });
        await publisher.PublishEventAsync(new TestEventOne());
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "Unhandled message" });
        await publisher.PublishEventAsync(new TestEventOne());
        await publisher.PublishEventAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(2);
        receivedBatches[1].Count.ShouldBe(2);
        completedBatches.ShouldBe(2);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }
}
