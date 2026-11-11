// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
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
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class StreamingTests
{
    [Fact]
    public async Task Streaming_ShouldReplayPendingMessagesInOrder_WhenConsumerIsStoppedAndRestarted()
    {
        int invocationsCount = 0;
        int tryCount = 0;
        List<string?> receivedMessages = [];
        TaskCompletionSource processingStartedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, CancellationToken>(HandleUnboundedStream));

        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref invocationsCount);

            await foreach (TestEventOne message in stream)
            {
                receivedMessages.Add(message.ContentEventOne);

                // Only the first attempt is canceled by the stop
                if (Interlocked.Increment(ref tryCount) > 1)
                    continue;

                processingStartedTaskCompletionSource.TrySetResult();
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await processingStartedTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(10));

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.StopAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        receivedMessages.ShouldBe(["1"]);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(3);

        // The canceled message is replayed from memory before the buffered ones
        await consumer.StartAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.ShouldBe(["1", "1", "2", "3"]);
        invocationsCount.ShouldBe(2);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Streaming_ShouldReplayPendingMessagesOfEachParallelExecution_WhenConsumerIsStoppedAndRestarted()
    {
        ConcurrentQueue<List<int>> receivedStreams = [];
        ConcurrentDictionary<int, bool> processedMessages = new();
        int startedCount = 0;
        TaskCompletionSource processingStartedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .EnableParallelProcessing(2)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>, CancellationToken>(HandleUnboundedStream));

        async Task HandleUnboundedStream(IMessageStreamEnumerable<TestEventOne> stream, CancellationToken cancellationToken)
        {
            List<int> list = [];
            receivedStreams.Enqueue(list);

            await foreach (TestEventOne message in stream)
            {
                int number = int.Parse(message.ContentEventOne!, CultureInfo.InvariantCulture);
                list.Add(number);

                // Only the first attempt of the first message of each parallel execution is canceled by the stop
                if (number > 2 || !processedMessages.TryAdd(number, true))
                    continue;

                if (Interlocked.Increment(ref startedCount) == 2)
                    processingStartedTaskCompletionSource.TrySetResult();

                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 4; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await processingStartedTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(10));

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.StopAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(4);

        // Each parallel execution replays its own canceled message before its buffered ones
        await consumer.StartAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedStreams.Select(list => list.ToArray())
            .OrderBy(list => list.Length)
            .ThenBy(list => list[0])
            .ShouldBe([[1], [2], [1, 3], [2, 4]]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }
}
