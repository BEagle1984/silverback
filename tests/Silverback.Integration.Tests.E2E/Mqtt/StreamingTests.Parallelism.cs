// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    public async Task Streaming_ShouldPublishStreamPerParallelExecution_WhenProcessingInParallel()
    {
        ConcurrentDictionary<IMessageStreamEnumerable<TestEventOne>, List<int>> receivedStreams = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .EnableParallelProcessing(3)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleUnboundedStream));

        async Task HandleUnboundedStream(IMessageStreamEnumerable<TestEventOne> stream)
        {
            List<int> list = receivedStreams.GetOrAdd(stream, _ => []);

            await foreach (TestEventOne message in stream)
            {
                list.Add(int.Parse(message.ContentEventOne!, CultureInfo.InvariantCulture));
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedStreams.Count.ShouldBe(3);

        // The messages are spread round-robin across the parallel executions, each processing its messages in order
        receivedStreams.Values.Select(list => list.ToArray())
            .OrderBy(list => list[0])
            .ShouldBe([[1, 4, 7, 10, 13], [2, 5, 8, 11, 14], [3, 6, 9, 12, 15]]);

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Streaming_ShouldPublishSingleStream_WhenNotProcessingInParallel()
    {
        List<TestEventOne> receivedMessages = [];
        ConcurrentBag<IMessageStreamEnumerable<TestEventOne>> receivedStreams = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .DisableParallelProcessing()
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleUnboundedStream));

        async Task HandleUnboundedStream(IMessageStreamEnumerable<TestEventOne> stream)
        {
            receivedStreams.Add(stream);

            await foreach (TestEventOne message in stream)
            {
                receivedMessages.Add(message);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedStreams.Count.ShouldBe(1);
        receivedMessages.Count.ShouldBe(15);
        receivedMessages.Select(message => message.ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Streaming_ShouldLimitParallelism()
    {
        TestingCollection<TestEventOne> receivedMessages = [];
        TaskCompletionSource<bool> taskCompletionSource = new();

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
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleUnboundedStream));

        async Task HandleUnboundedStream(IMessageStreamEnumerable<TestEventOne> stream)
        {
            await foreach (TestEventOne message in stream)
            {
                receivedMessages.Add(message);
                await taskCompletionSource.Task;
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 12; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);
        await Task.Delay(100);

        try
        {
            receivedMessages.Count.ShouldBe(2);
        }
        finally
        {
            taskCompletionSource.SetResult(true);
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Count.ShouldBe(12);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }
}
