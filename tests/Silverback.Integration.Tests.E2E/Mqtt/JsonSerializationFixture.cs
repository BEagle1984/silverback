// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class JsonSerializationFixture : MqttFixture
{
    public JsonSerializationFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task JsonSerialization_ShouldProduceAndConsume_WhenUsingDefaultSerializer()
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
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.OutboundEnvelopes[0].Headers[DefaultMessageHeaders.MessageType].ShouldBe(typeof(TestEventOne).AssemblyQualifiedName);
        Helper.Spy.OutboundEnvelopes[1].Headers[DefaultMessageHeaders.MessageType].ShouldBe(typeof(TestEventTwo).AssemblyQualifiedName);

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes[0].GetRawMessageAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E!\"}");
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Hello E2E!");
        Helper.Spy.InboundEnvelopes[1].GetRawMessageAsString().ShouldBe("{\"ContentEventTwo\":\"Hello E2E!\"}");
        Helper.Spy.InboundEnvelopes[1].Message.ShouldBeOfType<TestEventTwo>().ContentEventTwo.ShouldBe("Hello E2E!");
    }

    [Fact]
    public async Task JsonSerialization_ShouldProduceAndConsume_WhenUsingDefaultSerializerWithHardcodedType()
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
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.OutboundEnvelopes[0].Headers[DefaultMessageHeaders.MessageType].ShouldBe(typeof(TestEventOne).AssemblyQualifiedName);

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.InboundEnvelopes[0].GetRawMessageAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E!\"}");
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Hello E2E!");
    }

    [Fact]
    public async Task JsonSerialization_ShouldConsumeRegardlessOfTypeHeader_WhenUsingDefaultSerializerWithHardcodedType()
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
                .AddSingletonBrokerBehavior<RemoveMessageTypeHeaderProducerBehavior>()
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.OutboundEnvelopes[0].Headers.GetValue(DefaultMessageHeaders.MessageType).ShouldBeNull();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.InboundEnvelopes[0].GetRawMessageAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E!\"}");
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Hello E2E!");
    }

    [Fact]
    public async Task JsonSerialization_ShouldProduceAndConsume_WhenUsingNewtonsoftSerializer()
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
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName).SerializeAsJsonUsingNewtonsoft())
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).DeserializeJsonUsingNewtonsoft())))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.OutboundEnvelopes[0].Headers[DefaultMessageHeaders.MessageType].ShouldBe(typeof(TestEventOne).AssemblyQualifiedName);
        Helper.Spy.OutboundEnvelopes[1].Headers[DefaultMessageHeaders.MessageType].ShouldBe(typeof(TestEventTwo).AssemblyQualifiedName);

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes[0].GetRawMessageAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E!\"}");
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Hello E2E!");
        Helper.Spy.InboundEnvelopes[1].GetRawMessageAsString().ShouldBe("{\"ContentEventTwo\":\"Hello E2E!\"}");
        Helper.Spy.InboundEnvelopes[1].Message.ShouldBeOfType<TestEventTwo>().ContentEventTwo.ShouldBe("Hello E2E!");
    }

    [Fact]
    public async Task JsonSerialization_ShouldProduceAndConsume_WhenUsingNewtonsoftSerializerWithHardcodedType()
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
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName).SerializeAsJsonUsingNewtonsoft())
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName).DeserializeJsonUsingNewtonsoft())))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.OutboundEnvelopes[0].Headers[DefaultMessageHeaders.MessageType].ShouldBe(typeof(TestEventOne).AssemblyQualifiedName);

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.InboundEnvelopes[0].GetRawMessageAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E!\"}");
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Hello E2E!");
    }

    [Fact]
    public async Task JsonSerialization_ShouldConsumeRegardlessOfTypeHeader_WhenUsingNewtonsoftSerializerWithHardcodedType()
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
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName).DeserializeJsonUsingNewtonsoft())))
                .AddSingletonBrokerBehavior<RemoveMessageTypeHeaderProducerBehavior>()
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.OutboundEnvelopes[0].Headers.GetValue(DefaultMessageHeaders.MessageType).ShouldBeNull();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.InboundEnvelopes[0].GetRawMessageAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E!\"}");
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Hello E2E!");
    }
}
