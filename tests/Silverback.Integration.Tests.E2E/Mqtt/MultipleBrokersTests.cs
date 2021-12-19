// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class MultipleBrokersTests : MqttTestFixture
{
    public MultipleBrokersTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task MultipleBrokers_OverlappingTopicNames_CorrectlyProducedAndConsumed()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                    .AddMqttEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithClientId("e2e-test-1").ConnectViaTcp("e2e-mqtt-broker-1"))
                            .AddOutbound<Broker1Message>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(consumer => consumer.ConsumeFrom(DefaultTopicName)))
                    .AddMqttEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithClientId("e2e-test-2").ConnectViaTcp("e2e-mqtt-broker-2"))
                            .AddOutbound<Broker2Message>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(consumer => consumer.ConsumeFrom(DefaultTopicName)))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new Broker1Message());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        await publisher.PublishAsync(new Broker2Message());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<Broker1Message>();
        Helper.Spy.InboundEnvelopes[0]
            .Endpoint.As<MqttConsumerEndpoint>()
            .Configuration.Client.Channel.As<MqttClientTcpConfiguration>()
            .Server.Should().Be("e2e-mqtt-broker-1");
        Helper.Spy.InboundEnvelopes[1].Message.Should().BeOfType<Broker2Message>();
        Helper.Spy.InboundEnvelopes[1]
            .Endpoint.As<MqttConsumerEndpoint>()
            .Configuration.Client.Channel.As<MqttClientTcpConfiguration>()
            .Server.Should().Be("e2e-mqtt-broker-2");
    }

    private sealed class Broker1Message : IIntegrationMessage
    {
    }

    private sealed class Broker2Message : IIntegrationMessage
    {
    }
}
