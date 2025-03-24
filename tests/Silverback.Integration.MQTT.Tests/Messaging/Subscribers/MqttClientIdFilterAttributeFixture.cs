// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using MQTTnet;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Subscribers;

public class MqttClientIdFilterAttributeFixture
{
    [Theory]
    [InlineData("client1", true)]
    [InlineData("client2", true)]
    [InlineData("client3", false)]
    public void MustProcess_ShouldFilterByClientId(string consumerClientId, bool expectedResult)
    {
        IMqttClientWrapper clientWrapper = Substitute.For<IMqttClientWrapper>();
        clientWrapper.Connected.Returns(new AsyncEvent<BrokerClient>());
        clientWrapper.Disconnected.Returns(new AsyncEvent<BrokerClient>());
        clientWrapper.Initializing.Returns(new AsyncEvent<BrokerClient>());
        clientWrapper.Initialized.Returns(new AsyncEvent<BrokerClient>());
        clientWrapper.Disconnecting.Returns(new AsyncEvent<BrokerClient>());
        clientWrapper.MessageReceived.Returns(new AsyncEvent<MqttApplicationMessageReceivedEventArgs>());
        clientWrapper.Configuration.Returns(new MqttClientConfiguration());
        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ISequenceStore)).Returns(Substitute.For<ISequenceStore>());
        InboundEnvelope inboundEnvelope = new(
            new MemoryStream(),
            [],
            new MqttConsumerEndpoint(
                "my-topic",
                new MqttConsumerEndpointConfiguration()),
            new MqttConsumer(
                "42",
                clientWrapper,
                new MqttClientConfiguration
                {
                    ClientId = consumerClientId
                },
                Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
                serviceProvider,
                Substitute.For<IConsumerLogger<MqttConsumer>>()),
            new MqttMessageIdentifier(consumerClientId, "42"));

        bool result = new MqttClientIdFilterAttribute("client1", "client2").MustProcess(inboundEnvelope);

        result.ShouldBe(expectedResult);
    }

    [Fact]
    public void MustProcess_ShouldReturnFalse_WhenMessageIsNotInboundEnvelope()
    {
        bool result = new MqttClientIdFilterAttribute().MustProcess(new TestEventOne());

        result.ShouldBeFalse();
    }

    [Fact]
    public void MustProcess_ShouldReturnFalse_WhenConsumerIsNotMqttConsumer()
    {
        InboundEnvelope envelope = new(
            new MemoryStream(),
            [],
            new MqttConsumerEndpoint(
                "my-topic",
                new MqttConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new MqttMessageIdentifier("client1", "42"));

        bool result = new MqttClientIdFilterAttribute().MustProcess(envelope);

        result.ShouldBeFalse();
    }
}
