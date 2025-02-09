// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ErrorPoliciesFixture
{
    [Fact]
    public async Task MovePolicy_ShouldMoveMessageToOtherTopic()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo("other-topic"))
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.MoveTo("other-topic")))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        static void HandleMessage(IIntegrationEvent unused) => throw new InvalidOperationException("Move!");

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);

        Helper.Spy.OutboundEnvelopes[1].Message.ShouldBeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
        Helper.Spy.OutboundEnvelopes[1].GetEndpoint().RawName.ShouldBe("other-topic");

        IReadOnlyList<MqttApplicationMessage> messages = Helper.GetMessages("other-topic");
        messages.Count.ShouldBe(1);
        messages[0].Payload.ToArray().ShouldBe(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
    }

    [Fact]
    public async Task MovePolicy_ShouldMoveMessageToSameTopicAndRetry()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(
                                            policy => policy.MoveTo(
                                                DefaultTopicName,
                                                movePolicy => movePolicy.WithMaxRetries(10))))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(11);
        tryCount.ShouldBe(11);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(11);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.ShouldBeEquivalentTo(message));
    }

    [Fact]
    public async Task MovePolicy_ShouldMoveMessageToOtherTopicAfterRetry()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo("other-topic"))
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(1).ThenMoveTo("other-topic")))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);

        tryCount.ShouldBe(2);

        Helper.Spy.OutboundEnvelopes[1].Message.ShouldBeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
        Helper.Spy.OutboundEnvelopes[1].GetEndpoint().RawName.ShouldBe("other-topic");

        IReadOnlyList<MqttApplicationMessage> messages = Helper.GetMessages("other-topic");
        messages.Count.ShouldBe(1);
        messages[0].Payload.ToArray().ShouldBe(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
    }
}
