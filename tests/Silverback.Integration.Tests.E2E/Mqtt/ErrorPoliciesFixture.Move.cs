// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
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

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);

        Helper.Spy.OutboundEnvelopes[1].Message.Should().BeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
        Helper.Spy.OutboundEnvelopes[1].Endpoint.RawName.Should().Be("other-topic");

        IReadOnlyList<MqttApplicationMessage> messages = Helper.GetMessages("other-topic");
        messages.Should().HaveCount(1);
        messages[0].Payload.Should().BeEquivalentTo(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
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

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(11);
        tryCount.Should().Be(11);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(11);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
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

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

        tryCount.Should().Be(2);

        Helper.Spy.OutboundEnvelopes[1].Message.Should().BeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
        Helper.Spy.OutboundEnvelopes[1].Endpoint.RawName.Should().Be("other-topic");

        IReadOnlyList<MqttApplicationMessage> messages = Helper.GetMessages("other-topic");
        messages.Should().HaveCount(1);
        messages[0].Payload.Should().BeEquivalentTo(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
    }
}
