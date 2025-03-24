// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class BrokerClientsFixture : KafkaFixture
{
    public BrokerClientsFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task BrokerClients_ShouldBeAbleToConnectAndDisconnectManually()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5))
                        .ManuallyConnect())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            "producer",
                            producer => producer
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
                                .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic2")))
                        .AddConsumer(
                            "consumer1",
                            consumer => consumer
                                .WithGroupId("consume1")
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("topic1")))
                        .AddConsumer(
                            "consumer2",
                            consumer => consumer
                                .WithGroupId("consumer2")
                                .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("topic2"))))
                .AddIntegrationSpyAndSubscriber());

        IBrokerClientCollection clients = Host.ServiceProvider.GetRequiredService<IBrokerClientCollection>();
        IBrokerClient clientConsumer1 = clients["consumer1"];
        IBrokerClient clientConsumer2 = clients["consumer2"];
        IBrokerClient clientProducer = clients["producer"];

        clientConsumer1.Status.ShouldBe(ClientStatus.Disconnected);
        clientConsumer2.Status.ShouldBe(ClientStatus.Disconnected);
        clientProducer.Status.ShouldBe(ClientStatus.Disconnected);

        await clientProducer.ConnectAsync();

        clientConsumer1.Status.ShouldBe(ClientStatus.Disconnected);
        clientConsumer2.Status.ShouldBe(ClientStatus.Disconnected);
        clientProducer.Status.ShouldBe(ClientStatus.Initialized);

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());
        }

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(0);

        await clientConsumer1.ConnectAsync();

        clientConsumer1.Status.ShouldBe(ClientStatus.Initialized);
        clientConsumer2.Status.ShouldBe(ClientStatus.Disconnected);
        clientProducer.Status.ShouldBe(ClientStatus.Initialized);

        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.InboundEnvelopes.Count == 5);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(5);

        await clientConsumer2.ConnectAsync();
        await clientConsumer1.DisconnectAsync();

        clientConsumer1.Status.ShouldBe(ClientStatus.Disconnected);
        clientConsumer2.Status.ShouldBe(ClientStatus.Initialized);
        clientProducer.Status.ShouldBe(ClientStatus.Initialized);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(10);

        await clientConsumer2.DisconnectAsync();
        await clientProducer.DisconnectAsync();

        clientConsumer1.Status.ShouldBe(ClientStatus.Disconnected);
        clientConsumer2.Status.ShouldBe(ClientStatus.Disconnected);
        clientProducer.Status.ShouldBe(ClientStatus.Disconnected);
    }
}
