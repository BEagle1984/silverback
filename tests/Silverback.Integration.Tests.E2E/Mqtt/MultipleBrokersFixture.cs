// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class MultipleBrokersFixture : MqttFixture
{
    public MultipleBrokersFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task MultipleBrokers_ShouldCorrectlyProduceAndConsume_WhenTopicNamesAreOverlapping()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker-1")
                        .AddClient(
                            client => client
                                .WithClientId("client1")
                                .Produce<Broker1Message>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker-2")
                        .AddClient(
                            client => client
                                .WithClientId("client2")
                                .Produce<Broker2Message>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new Broker1Message());
        await publisher.PublishAsync(new Broker2Message());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<Broker1Message>();
        Helper.Spy.InboundEnvelopes[0].Consumer.As<MqttConsumer>().Configuration.Channel.As<MqttClientTcpConfiguration>().Server
            .Should().Be("e2e-mqtt-broker-1");
        Helper.Spy.InboundEnvelopes[1].Message.Should().BeOfType<Broker2Message>();
        Helper.Spy.InboundEnvelopes[1].Consumer.As<MqttConsumer>().Configuration.Channel.As<MqttClientTcpConfiguration>().Server
            .Should().Be("e2e-mqtt-broker-2");
    }

    [Fact]
    public async Task MultipleBrokers_ShouldCorrectlyProduceAndConsume_WhenTopicNamesAndClientIdsAreOverlapping()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker-1")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Produce<Broker1Message>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker-2")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Produce<Broker2Message>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new Broker1Message());
        await publisher.PublishAsync(new Broker2Message());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<Broker1Message>();
        Helper.Spy.InboundEnvelopes[0].Consumer.As<MqttConsumer>().Configuration.Channel.As<MqttClientTcpConfiguration>().Server
            .Should().Be("e2e-mqtt-broker-1");
        Helper.Spy.InboundEnvelopes[1].Message.Should().BeOfType<Broker2Message>();
        Helper.Spy.InboundEnvelopes[1].Consumer.As<MqttConsumer>().Configuration.Channel.As<MqttClientTcpConfiguration>().Server
            .Should().Be("e2e-mqtt-broker-2");
    }

    private sealed class Broker1Message : IIntegrationMessage
    {
    }

    private sealed class Broker2Message : IIntegrationMessage
    {
    }
}
