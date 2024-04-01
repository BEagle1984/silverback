// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ConsumerEndpointFixture : MqttFixture
{
    public ConsumerEndpointFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeSequentially()
    {
        int receivedMessages = 0;
        int exitedSubscribers = 0;
        bool areOverlapping = false;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<TestEventOne>(HandleMessage));

        async ValueTask HandleMessage(TestEventOne message)
        {
            if (receivedMessages != exitedSubscribers)
                areOverlapping = true;

            receivedMessages++;

            await Task.Delay(100);

            exitedSubscribers++;
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 10; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        areOverlapping.Should().BeFalse();
        receivedMessages.Should().Be(10);
        exitedSubscribers.Should().Be(10);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsume_WhenMultipleClientsSubscribeDifferentTopics()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId("client1")
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("topic1")))
                        .AddClient(
                            client => client
                                .WithClientId("client2")
                                .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("topic2"))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer1 = Helper.GetProducerForEndpoint("topic1");
        IProducer producer2 = Helper.GetProducerForEndpoint("topic2");

        for (int i = 1; i <= 5; i++)
        {
            await producer1.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await producer2.ProduceAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(10);

        IInboundEnvelope<TestEventOne>[] eventOneEnvelopes = Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventOne>>().ToArray();
        IInboundEnvelope<TestEventTwo>[] eventTwoEnvelopes = Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventTwo>>().ToArray();

        eventOneEnvelopes.Should().HaveCount(5);
        eventOneEnvelopes.Select(envelope => envelope.Endpoint.RawName).Should().AllBe("topic1");
        eventOneEnvelopes.Select(envelope => envelope.Message?.ContentEventOne).Should().BeEquivalentTo(["1", "2", "3", "4", "5"], options => options.WithoutStrictOrdering());
        eventTwoEnvelopes.Should().HaveCount(5);
        eventTwoEnvelopes.Select(envelope => envelope.Endpoint.RawName).Should().AllBe("topic2");
        eventTwoEnvelopes.Select(envelope => envelope.Message?.ContentEventTwo).Should().BeEquivalentTo(["1", "2", "3", "4", "5"], options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsume_WhenSingleClientSubscribesMultipleTopics()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId("client1")
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("topic1"))
                                .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("topic2"))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer1 = Helper.GetProducerForEndpoint("topic1");
        IProducer producer2 = Helper.GetProducerForEndpoint("topic2");

        for (int i = 1; i <= 5; i++)
        {
            await producer1.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await producer2.ProduceAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(10);

        IInboundEnvelope<TestEventOne>[] eventOneEnvelopes = Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventOne>>().ToArray();
        IInboundEnvelope<TestEventTwo>[] eventTwoEnvelopes = Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventTwo>>().ToArray();

        eventOneEnvelopes.Should().HaveCount(5);
        eventOneEnvelopes.Select(envelope => envelope.Endpoint.RawName).Should().AllBe("topic1");
        eventOneEnvelopes.Select(envelope => envelope.Message?.ContentEventOne).Should().BeEquivalentTo("1", "2", "3", "4", "5");
        eventTwoEnvelopes.Should().HaveCount(5);
        eventTwoEnvelopes.Select(envelope => envelope.Endpoint.RawName).Should().AllBe("topic2");
        eventTwoEnvelopes.Select(envelope => envelope.Message?.ContentEventTwo).Should().BeEquivalentTo("1", "2", "3", "4", "5");
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeTwice_WhenMultipleClientsSubscribeTheSameTopic()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId("client1")
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                        .AddClient(
                            client => client
                                .WithClientId("client2")
                                .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        MqttConsumer[] consumers = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<MqttConsumer>().ToArray();
        consumers.Should().HaveCount(2);

        IInboundEnvelope<TestEventOne>[] inboundEnvelopes = Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventOne>>().ToArray();
        inboundEnvelopes.Should().HaveCount(10);

        IInboundEnvelope<TestEventOne>[] consumer1Envelopes = inboundEnvelopes.Where(envelope => envelope.Consumer == consumers[0]).ToArray();
        IInboundEnvelope<TestEventOne>[] consumer2Envelopes = inboundEnvelopes.Where(envelope => envelope.Consumer == consumers[1]).ToArray();

        consumer1Envelopes.Select(envelope => envelope.Message?.ContentEventOne).Should().BeEquivalentTo("1", "2", "3", "4", "5");
        consumer2Envelopes.Select(envelope => envelope.Message?.ContentEventOne).Should().BeEquivalentTo("1", "2", "3", "4", "5");
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsume_WhenMultipleClientsSubscribeTheSameTopicUsingSharedSession()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId("client1")
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom($"$share/group1/{DefaultTopicName}")))
                        .AddClient(
                            client => client
                                .WithClientId("client2")
                                .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom($"$share/group1/{DefaultTopicName}")))
                        .AddClient(
                            client => client
                                .WithClientId("client3")
                                .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom($"$share/group2/{DefaultTopicName}")))
                        .AddClient(
                            client => client
                                .WithClientId("client4")
                                .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        MqttConsumer[] consumers = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<MqttConsumer>().ToArray();
        consumers.Should().HaveCount(4);

        IInboundEnvelope<TestEventOne>[] inboundEnvelopes = Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventOne>>().ToArray();
        inboundEnvelopes.Should().HaveCount(15);

        IInboundEnvelope<TestEventOne>[] client1Envelopes = inboundEnvelopes.Where(
            envelope => envelope
                .Consumer.As<MqttConsumer>().Configuration.ClientId == "client1").ToArray();
        IInboundEnvelope<TestEventOne>[] client2Envelopes = inboundEnvelopes.Where(
            envelope => envelope
                .Consumer.As<MqttConsumer>().Configuration.ClientId == "client2").ToArray();
        IInboundEnvelope<TestEventOne>[] client3Envelopes = inboundEnvelopes.Where(
            envelope => envelope
                .Consumer.As<MqttConsumer>().Configuration.ClientId == "client3").ToArray();
        IInboundEnvelope<TestEventOne>[] client4Envelopes = inboundEnvelopes.Where(
            envelope => envelope
                .Consumer.As<MqttConsumer>().Configuration.ClientId == "client4").ToArray();

        client1Envelopes.Union(client2Envelopes).Select(envelope => envelope.Message?.ContentEventOne).Should().BeEquivalentTo("1", "2", "3", "4", "5");
        client3Envelopes.Select(envelope => envelope.Message?.ContentEventOne).Should().BeEquivalentTo("1", "2", "3", "4", "5");
        client4Envelopes.Select(envelope => envelope.Message?.ContentEventOne).Should().BeEquivalentTo("1", "2", "3", "4", "5");
    }

    // TODO: Check flaky test
    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeAfterStopAndStart()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.Should().Be(2);

        await consumer.StopAsync();

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Task.Delay(100);

        await consumer.StartAsync();

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.Should().Be(4);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumePersistentSessionAfterDisconnectAndReconnect()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .RequestPersistentSession()
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.Should().Be(2);

        await consumer.Client.DisconnectAsync();

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "4" });

        await consumer.Client.ConnectAsync();

        await Helper.WaitUntilConnectedAsync();
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.Should().Be(4);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsume_WhenTopicNameContainsSpecialCharacters()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId("e2e-test-1")
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("test/+")))
                        .AddClient(
                            client => client
                                .WithClientId("e2e-test-2")
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("test/#"))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducer(
            client => client
                .ConnectViaTcp("e2e-mqtt-broker")
                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo("test/some-topic?name")));

        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
    }
}
