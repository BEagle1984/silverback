// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class StreamingTests : MqttTests
{
    public StreamingTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Streaming_ShouldConsumeAndAcknowledge_WhenSubscribingToStream()
    {
        List<TestEventOne> receivedMessages = [];
        HoldMessageBehavior holdMessageBehavior = new("1");

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
            .AddSingletonBrokerBehavior(holdMessageBehavior)
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleUnboundedStream));

        // All messages are delivered while the first one is held: each message is acknowledged before the next one is pushed
        void HandleUnboundedStream(IMessageStreamEnumerable<TestEventOne> stream)
        {
            foreach (TestEventOne message in stream)
            {
                DefaultClientSession.GetPendingMessagesCount().ShouldBe(15 - receivedMessages.Count);
                receivedMessages.Add(message);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        holdMessageBehavior.Release();
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Count.ShouldBe(15);
        receivedMessages.Select(message => message.ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Streaming_ShouldConsumeAndAcknowledge_WhenSubscribingToEnvelopesStream()
    {
        List<TestEventOne> receivedMessages = [];
        HoldMessageBehavior holdMessageBehavior = new("1");

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
            .AddSingletonBrokerBehavior(holdMessageBehavior)
            .AddDelegateSubscriber<IMessageStreamEnumerable<IInboundEnvelope<TestEventOne>>>(HandleUnboundedStream));

        void HandleUnboundedStream(IMessageStreamEnumerable<IInboundEnvelope<TestEventOne>> stream)
        {
            foreach (IInboundEnvelope<TestEventOne> envelope in stream)
            {
                DefaultClientSession.GetPendingMessagesCount().ShouldBe(15 - receivedMessages.Count);
                receivedMessages.Add(envelope.Message!);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        holdMessageBehavior.Release();
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Count.ShouldBe(15);
        receivedMessages.Select(message => message.ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Streaming_ShouldConsumeAndAcknowledge_WhenSubscribingToStreamAndEnumeratingAsynchronously()
    {
        List<TestEventOne> receivedMessages = [];
        HoldMessageBehavior holdMessageBehavior = new("1");

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
            .AddSingletonBrokerBehavior(holdMessageBehavior)
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleUnboundedStream));

        async Task HandleUnboundedStream(IMessageStreamEnumerable<TestEventOne> stream)
        {
            await foreach (TestEventOne message in stream)
            {
                DefaultClientSession.GetPendingMessagesCount().ShouldBe(15 - receivedMessages.Count);
                receivedMessages.Add(message);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        holdMessageBehavior.Release();
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Count.ShouldBe(15);
        receivedMessages.Select(message => message.ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Streaming_ShouldConsumeAndAcknowledge_WhenSubscribingToEnumerable()
    {
        List<TestEventOne> receivedMessages = [];
        HoldMessageBehavior holdMessageBehavior = new("1");

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
            .AddSingletonBrokerBehavior(holdMessageBehavior)
            .AddDelegateSubscriber<IEnumerable<TestEventOne>>(HandleUnboundedStream));

        void HandleUnboundedStream(IEnumerable<TestEventOne> stream)
        {
            foreach (TestEventOne message in stream)
            {
                DefaultClientSession.GetPendingMessagesCount().ShouldBe(15 - receivedMessages.Count);
                receivedMessages.Add(message);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        holdMessageBehavior.Release();
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Count.ShouldBe(15);
        receivedMessages.Select(message => message.ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Streaming_ShouldConsumeAndAcknowledge_WhenSubscribingToAsyncEnumerable()
    {
        List<TestEventOne> receivedMessages = [];
        HoldMessageBehavior holdMessageBehavior = new("1");

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
            .AddSingletonBrokerBehavior(holdMessageBehavior)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream));

        async ValueTask HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream)
        {
            await foreach (TestEventOne message in stream)
            {
                DefaultClientSession.GetPendingMessagesCount().ShouldBe(15 - receivedMessages.Count);
                receivedMessages.Add(message);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        holdMessageBehavior.Release();
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Count.ShouldBe(15);
        receivedMessages.Select(message => message.ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Streaming_ShouldConsumeAndAcknowledge_WhenSubscribingToObservable()
    {
        List<TestEventOne> receivedMessages = [];
        HoldMessageBehavior holdMessageBehavior = new("1");

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .AsObservable()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
            .AddSingletonBrokerBehavior(holdMessageBehavior)
            .AddDelegateSubscriber<IMessageStreamObservable<TestEventOne>>(HandleUnboundedStream));

        void HandleUnboundedStream(IMessageStreamObservable<TestEventOne> stream) =>
            stream.Subscribe(message =>
            {
                DefaultClientSession.GetPendingMessagesCount().ShouldBe(15 - receivedMessages.Count);
                receivedMessages.Add(message);
            });

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        holdMessageBehavior.Release();
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Count.ShouldBe(15);
        receivedMessages.Select(message => message.ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }
}
