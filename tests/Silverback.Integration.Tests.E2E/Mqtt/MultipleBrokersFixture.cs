// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new Broker1Message());
        await Helper.WaitUntilAllMessagesAreConsumedAsync(); // Wait twice to ensure ordering in asserts

        await publisher.PublishAsync(new Broker2Message());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<Broker1Message>();
        Helper.Spy.InboundEnvelopes[0]
            .Consumer.ShouldBeOfType<MqttConsumer>()
            .Configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>()
            .RemoteEndpoint.ShouldBeOfType<DnsEndPoint>()
            .Host.ShouldBe("e2e-mqtt-broker-1");
        Helper.Spy.InboundEnvelopes[1].Message.ShouldBeOfType<Broker2Message>();
        Helper.Spy.InboundEnvelopes[1]
            .Consumer.ShouldBeOfType<MqttConsumer>()
            .Configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>()
            .RemoteEndpoint.ShouldBeOfType<DnsEndPoint>()
            .Host.ShouldBe("e2e-mqtt-broker-2");
    }

    [Fact]
    public async Task MultipleBrokers_ShouldCorrectlyProduceAndConsume_WhenTopicNamesAndClientIdsAreOverlapping()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
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

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new Broker1Message());
        await Helper.WaitUntilAllMessagesAreConsumedAsync(); // Wait twice to ensure ordering in asserts

        await publisher.PublishAsync(new Broker2Message());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<Broker1Message>();
        Helper.Spy.InboundEnvelopes[0]
            .Consumer.ShouldBeOfType<MqttConsumer>()
            .Configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>()
            .RemoteEndpoint.ShouldBeOfType<DnsEndPoint>()
            .Host.ShouldBe("e2e-mqtt-broker-1");
        Helper.Spy.InboundEnvelopes[1].Message.ShouldBeOfType<Broker2Message>();
        Helper.Spy.InboundEnvelopes[1]
            .Consumer.ShouldBeOfType<MqttConsumer>()
            .Configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>()
            .RemoteEndpoint.ShouldBeOfType<DnsEndPoint>()
            .Host.ShouldBe("e2e-mqtt-broker-2");
    }

    private sealed class Broker1Message : IIntegrationMessage;

    private sealed class Broker2Message : IIntegrationMessage;
}
