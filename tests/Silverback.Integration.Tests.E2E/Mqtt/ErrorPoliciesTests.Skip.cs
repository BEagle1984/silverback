// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ErrorPoliciesTests
{
    [Fact]
    public async Task SkipPolicy_ShouldSkipMessage_WhenJsonDeserializationFails()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        byte[] invalidRawMessage = Encoding.UTF8.GetBytes("<what?!>");

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.Skip()))))
            .AddIntegrationSpy());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            invalidRawMessage,
            new MessageHeaderCollection
            {
                { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
            });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.ShouldBeEmpty();
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);

        await producer.RawProduceAsync(
            rawMessage,
            new MessageHeaderCollection
            {
                { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
            });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task SkipPolicy_ShouldSkipMessage_WhenOtherEndpointIsStreaming()
    {
        int streamsCount = 0;
        ConcurrentQueue<string?> streamedMessages = [];
        ConcurrentQueue<string?> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom("e2e/stream")
                        .AllowStreaming())
                    .Consume<TestEventTwo>(endpoint => endpoint
                        .ConsumeFrom("e2e/skip")
                        .OnError(policy => policy.Skip()))))
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddDelegateSubscriber<TestEventTwo>(HandleMessage));

        void HandleUnboundedStream(IMessageStreamEnumerable<TestEventOne> stream)
        {
            Interlocked.Increment(ref streamsCount);

            foreach (TestEventOne message in stream)
            {
                streamedMessages.Enqueue(message.ContentEventOne);
            }
        }

        void HandleMessage(TestEventTwo message)
        {
            if (message.ContentEventTwo == "fail")
                throw new InvalidOperationException("Test");

            receivedMessages.Enqueue(message.ContentEventTwo);
        }

        IProducer streamProducer = Helper.GetProducerForEndpoint("e2e/stream");
        IProducer skipProducer = Helper.GetProducerForEndpoint("e2e/skip");

        await streamProducer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await AsyncTestingUtil.WaitAsync(() => !streamedMessages.IsEmpty);

        await skipProducer.ProduceAsync(new TestEventTwo { ContentEventTwo = "fail" });
        await skipProducer.ProduceAsync(new TestEventTwo { ContentEventTwo = "2" });
        await streamProducer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        streamsCount.ShouldBe(1);
        streamedMessages.ShouldBe(["1", "3"]);
        receivedMessages.ShouldBe(["2"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }
}
