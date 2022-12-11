// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.Subscriptions;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class ClientIdFilterFixture : MqttFixture
{
    public ClientIdFilterFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task ClientIdFilterAttribute_ShouldFilterAccordingToClientId_WhenSubscriberDecorated()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(client => client.WithClientId("client1").Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                        .AddClient(client => client.WithClientId("client2").Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddSingletonSubscriber<DecoratedSubscriber>()
                .AddIntegrationSpy());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);

        DecoratedSubscriber subscriber = Host.ServiceProvider.GetRequiredService<DecoratedSubscriber>();
        subscriber.ReceivedConsumer1.Should().Be(3);
        subscriber.ReceivedConsumer2.Should().Be(3);
    }

    [Fact]
    public async Task ClientIdFilterAttribute_ShouldFilterAccordingToClientId_WhenAddedViaConfiguration()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(client => client.WithClientId("client1").Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                        .AddClient(client => client.WithClientId("client2").Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddSingletonSubscriber<Subscriber>(
                    new TypeSubscriptionOptions
                    {
                        Filters = new[]
                        {
                            new MqttClientIdFilterAttribute("client1")
                        }
                    })
                .AddIntegrationSpy());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);

        Subscriber subscriber = Host.ServiceProvider.GetRequiredService<Subscriber>();
        subscriber.Received.Should().Be(3);
    }

    [Fact]
    public async Task ClientIdFilterAttribute_ShouldFilterAccordingToClientId_WhenFilterAddedToDelegateSubscriber()
    {
        int received1 = 0;
        int received2 = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(client => client.WithClientId("client1").Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                        .AddClient(client => client.WithClientId("client2").Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IEvent>(
                    HandleEventclient1,
                    new DelegateSubscriptionOptions
                    {
                        Filters = new[]
                        {
                            new MqttClientIdFilterAttribute("client1")
                        }
                    })
                .AddDelegateSubscriber<IEvent>(
                    HandleEventclient2,
                    new DelegateSubscriptionOptions
                    {
                        Filters = new[]
                        {
                            new MqttClientIdFilterAttribute("client2")
                        }
                    })
                .AddIntegrationSpy());

        void HandleEventclient1(IEvent message) => Interlocked.Increment(ref received1);
        void HandleEventclient2(IEvent message) => Interlocked.Increment(ref received2);

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);

        received1.Should().Be(3);
        received2.Should().Be(3);
    }

    [UsedImplicitly]
    [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
    private sealed class Subscriber
    {
        private int _received;

        public int Received => _received;

        [SuppressMessage(
            "ReSharper",
            "UnusedMember.Local",
            Justification = Justifications.CalledBySilverback)]
        [SuppressMessage(
            "ReSharper",
            "UnusedParameter.Local",
            Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
        public void OnMessageReceived(IMessage message) =>
            Interlocked.Increment(ref _received);
    }

    [UsedImplicitly]
    [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
    private sealed class DecoratedSubscriber
    {
        private int _receivedConsumer1;

        private int _receivedConsumer2;

        public int ReceivedConsumer1 => _receivedConsumer1;

        public int ReceivedConsumer2 => _receivedConsumer2;

        [MqttClientIdFilter("client1")]
        [SuppressMessage(
            "ReSharper",
            "UnusedMember.Local",
            Justification = Justifications.CalledBySilverback)]
        [SuppressMessage(
            "ReSharper",
            "UnusedParameter.Local",
            Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
        public void OnConsumer1Received(IMessage message) => Interlocked.Increment(ref _receivedConsumer1);

        [MqttClientIdFilter("client2")]
        [SuppressMessage(
            "ReSharper",
            "UnusedMember.Local",
            Justification = Justifications.CalledBySilverback)]
        [SuppressMessage(
            "ReSharper",
            "UnusedParameter.Local",
            Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
        public void OnConsumer2Received(IMessage message) => Interlocked.Increment(ref _receivedConsumer2);
    }
}
