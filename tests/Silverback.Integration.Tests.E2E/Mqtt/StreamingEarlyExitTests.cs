// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class StreamingEarlyExitTests : MqttTests
{
    private const string Topic1 = "e2e/stream1";

    private const string Topic2 = "e2e/stream2";

    public StreamingEarlyExitTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public Task Streaming_ShouldPublishNewStreamToSubscriber_WhenEnumerationIsAbortedWhileOtherSubscriberIsEnumerating() =>
        PublishNewStreamWhenEnumerationIsAbortedAsync(client => client
            .Consume(endpoint => endpoint
                .ConsumeFrom(Topic1, Topic2)
                .AllowStreaming()));

    [Fact]
    public Task Streaming_ShouldPublishNewStreamToSubscriber_WhenEnumerationIsAbortedWhileSubscriberOfOtherEndpointIsEnumerating() =>
        PublishNewStreamWhenEnumerationIsAbortedAsync(client => client
            .Consume<TestEventOne>(endpoint => endpoint
                .ConsumeFrom(Topic1)
                .AllowStreaming())
            .Consume<TestEventTwo>(endpoint => endpoint
                .ConsumeFrom(Topic2)
                .AllowStreaming()));

    private async Task PublishNewStreamWhenEnumerationIsAbortedAsync(Action<MqttClientConfigurationBuilder> configureConsumer)
    {
        int invocationsCount = 0;
        int otherInvocationsCount = 0;
        List<string?> receivedMessages = [];
        List<string?> otherReceivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId("producer")
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(Topic1))
                    .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo(Topic2)))
                .AddClient(client =>
                {
                    client.WithClientId(DefaultClientId);
                    configureConsumer(client);
                }))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventTwo>>(HandleOtherStream));

        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream)
        {
            Interlocked.Increment(ref invocationsCount);

            await foreach (TestEventOne message in stream)
            {
                receivedMessages.Add(message.ContentEventOne);

                if (message.ContentEventOne == "2")
                    break;
            }
        }

        async Task HandleOtherStream(IAsyncEnumerable<TestEventTwo> stream)
        {
            Interlocked.Increment(ref otherInvocationsCount);

            await foreach (TestEventTwo message in stream)
            {
                otherReceivedMessages.Add(message.ContentEventTwo);
            }
        }

        IProducer producer1 = Helper.GetProducerForEndpoint(Topic1);
        IProducer producer2 = Helper.GetProducerForEndpoint(Topic2);

        for (int i = 1; i <= 4; i++)
        {
            await producer1.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await producer2.ProduceAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.ShouldBe(["1", "2", "3", "4"]);
        invocationsCount.ShouldBe(2);
        otherReceivedMessages.ShouldBe(["1", "2", "3", "4"]);
        otherInvocationsCount.ShouldBe(1);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }
}
