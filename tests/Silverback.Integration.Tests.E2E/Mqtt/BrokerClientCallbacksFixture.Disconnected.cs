// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
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
    public async Task DisconnectingCallback_ShouldBeInvoked()
    {
        TestDisconnectingCallback callback = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
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

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        await consumer.Client.DisconnectAsync();

        await AsyncTestingUtil.WaitAsync(() => callback.CallsCount > 0);

        callback.CallsCount.Should().Be(1);
    }

    [Fact]
    public async Task DisconnectingCallback_ShouldProduceFromWithinTheCallback()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId).ConnectViaTcp("e2e-mqtt-broker")
                                .Produce<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                                .Consume(consumer => consumer.ConsumeFrom(DefaultTopicName))))
                .AddScopedBrokerClientCallback<SendMessageDisconnectingCallback>()
                .AddIntegrationSpyAndSubscriber());

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        await consumer.Client.DisconnectAsync();

        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.OutboundEnvelopes.Count > 0);

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
    }

    private sealed class TestDisconnectingCallback : IMqttClientDisconnectingCallback
    {
        public int CallsCount { get; private set; }

        public Task OnClientDisconnectingAsync(MqttClientConfiguration configuration)
        {
            CallsCount++;
            return Task.CompletedTask;
        }
    }

    [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
    private sealed class SendMessageDisconnectingCallback : IMqttClientDisconnectingCallback
    {
        private readonly IPublisher _publisher;

        public SendMessageDisconnectingCallback(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public Task OnClientDisconnectingAsync(MqttClientConfiguration configuration) =>
            _publisher.PublishAsync(new TestEventOne()).AsTask();
    }
}
