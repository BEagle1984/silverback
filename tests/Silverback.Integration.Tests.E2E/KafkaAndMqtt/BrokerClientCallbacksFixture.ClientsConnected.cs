// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Testing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.KafkaAndMqtt;

public partial class BrokerClientCallbacksFixture
{
    [Fact]
    public async Task ClientsConnectedCallback_ShouldBeInvokedOnce()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .AddSingletonBrokerClientCallback<BrokerClientsConnectedCallback>()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId("e2e-client")
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))));

        IKafkaTestingHelper kafkaTestingHelper = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();
        await kafkaTestingHelper.WaitUntilConnectedAsync();

        BrokerClientsConnectedCallback callback = (BrokerClientsConnectedCallback)Host.ServiceProvider
            .GetServices<IBrokerClientCallback>()
            .Single(callback => callback is BrokerClientsConnectedCallback);
        callback.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task ClientsConnectedCallback_ShouldInvokeAllRegisteredHandlers()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .AddSingletonBrokerClientCallback<BrokerClientsConnectedCallback>()
                .AddSingletonBrokerClientCallback<OtherBrokerClientsConnectedCallback>()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))));

        IKafkaTestingHelper kafkaTestingHelper = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();
        await kafkaTestingHelper.WaitUntilConnectedAsync();

        List<IBrokerClientCallback> callbacks = Host.ServiceProvider
            .GetServices<IBrokerClientCallback>()
            .Where(service => service is BrokerClientsConnectedCallback)
            .ToList();
        callbacks.Should().HaveCount(2);
        callbacks.Cast<BrokerClientsConnectedCallback>().Should().OnlyContain(callback => callback.CallCount == 1);
    }

    [Fact]
    public async Task ClientsConnectedCallback_ShouldBeAbleToProduceFromWithinTheCallback()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .AddScopedBrokerClientCallback<ProducingBrokerClientsConnectedCallback>()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IKafkaTestingHelper kafkaTestingHelper = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();
        await kafkaTestingHelper.WaitUntilConnectedAsync();
        await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

        kafkaTestingHelper.Spy.OutboundEnvelopes.Should().HaveCount(1);
    }

    private class BrokerClientsConnectedCallback : IBrokerClientsConnectedCallback
    {
        public int CallCount { get; private set; }

        public Task OnBrokerClientsConnectedAsync()
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class OtherBrokerClientsConnectedCallback : BrokerClientsConnectedCallback;

    private sealed class ProducingBrokerClientsConnectedCallback : IBrokerClientsConnectedCallback
    {
        private readonly IPublisher _publisher;

        public ProducingBrokerClientsConnectedCallback(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public async Task OnBrokerClientsConnectedAsync()
        {
            TestEventOne message = new();
            await _publisher.PublishAsync(message);
        }
    }
}
