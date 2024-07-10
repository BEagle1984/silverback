// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class BrokerClientCallbacksFixture
{
    [Fact]
    public async Task ConnectedCallback_ShouldBeInvoked()
    {
        TestConnectedCallback callback = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId).ConnectViaTcp("e2e-mqtt-broker")
                                .Produce<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                                .Consume(consumer => consumer.ConsumeFrom(DefaultTopicName))))
                .AddSingletonBrokerClientCallback(callback)
                .AddIntegrationSpyAndSubscriber());

        await AsyncTestingUtil.WaitAsync(() => callback.CallsCount > 0);

        callback.CallsCount.Should().Be(1);
    }

    [Fact]
    public async Task ConnectedCallback_ShouldProduceFromWithinTheCallback()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId).ConnectViaTcp("e2e-mqtt-broker")
                                .Produce<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                                .Consume(consumer => consumer.ConsumeFrom(DefaultTopicName))))
                .AddScopedBrokerClientCallback<SendMessageConnectedCallback>()
                .AddIntegrationSpyAndSubscriber());

        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.InboundEnvelopes.Count > 0);

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
    }

    private sealed class TestConnectedCallback : IMqttClientConnectedCallback
    {
        public int CallsCount { get; private set; }

        public Task OnClientConnectedAsync(MqttClientConfiguration configuration)
        {
            CallsCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class SendMessageConnectedCallback : IMqttClientConnectedCallback
    {
        private readonly IPublisher _publisher;

        public SendMessageConnectedCallback(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public Task OnClientConnectedAsync(MqttClientConfiguration configuration) =>
            _publisher.PublishAsync(new TestEventOne());
    }
}
