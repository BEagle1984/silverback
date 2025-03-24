// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration;
using Silverback.Testing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.KafkaAndMqtt;

public partial class BrokerClientCallbacksFixture
{
    [Fact]
    public async Task ClientsConfiguredCallback_ShouldBeInvokedOnce()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .AddSingletonBrokerClientCallback<BrokerClientsConfiguredCallback>()
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

        BrokerClientsConfiguredCallback callback = (BrokerClientsConfiguredCallback)Host.ServiceProvider
            .GetServices<IBrokerClientCallback>()
            .Single(callback => callback is BrokerClientsConfiguredCallback);
        callback.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task ClientsConfiguredCallback_ShouldInvokeAllRegisteredHandlers()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .AddSingletonBrokerClientCallback<BrokerClientsConfiguredCallback>()
                .AddSingletonBrokerClientCallback<OtherBrokerClientsConfiguredCallback>()
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
            .Where(service => service is BrokerClientsConfiguredCallback)
            .ToList();
        callbacks.Count.ShouldBe(2);
        callbacks.Cast<BrokerClientsConfiguredCallback>().ShouldAllBe(callback => callback.CallCount == 1);
    }

    private class BrokerClientsConfiguredCallback : IBrokerClientsConfiguredCallback
    {
        public int CallCount { get; private set; }

        public Task OnBrokerClientsConfiguredAsync()
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class OtherBrokerClientsConfiguredCallback : BrokerClientsConfiguredCallback;
}
