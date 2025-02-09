// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Packets;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ProducerEndpointFixture : MqttFixture
{
    public ProducerEndpointFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldSerializeAndProduce()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

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
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(message);

        IReadOnlyList<MqttApplicationMessage> messages = GetDefaultTopicMessages();
        messages.Count.ShouldBe(1);
        messages[0].Payload.ToArray().ShouldBe(rawMessage);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldSerializeAndProduce_WhenMultipleClientsForDifferentMessagesAreConfigured()
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
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1")))
                        .AddClient(
                            client => client
                                .WithClientId("client2")
                                .Produce<TestEventTwo>(
                                    endpoint => endpoint.ProduceTo("topic2").SerializeAsJson(
                                        serializer => serializer
                                            .Configure(
                                                options =>
                                                {
                                                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                                                }))))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
            await publisher.PublishEventAsync(new TestEventThree { ContentEventThree = $"{i}" });
        }

        Host.ServiceProvider.GetRequiredService<IProducerCollection>().Count.ShouldBe(2);
        Helper.GetMessages("topic1").GetContentAsString().ShouldBe(
            [
                "{\"ContentEventOne\":\"1\"}",
                "{\"ContentEventOne\":\"2\"}",
                "{\"ContentEventOne\":\"3\"}",
                "{\"ContentEventOne\":\"4\"}",
                "{\"ContentEventOne\":\"5\"}"
            ],
            ignoreOrder: true);
        Helper.GetMessages("topic2").GetContentAsString().ShouldBe(
            [
                "{\"contentEventTwo\":\"1\"}",
                "{\"contentEventTwo\":\"2\"}",
                "{\"contentEventTwo\":\"3\"}",
                "{\"contentEventTwo\":\"4\"}",
                "{\"contentEventTwo\":\"5\"}"
            ],
            ignoreOrder: true);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldSerializeAndProduce_WhenSingleClientHandlesMultipleTypes()
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
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
                                .Produce<TestEventTwo>(
                                    endpoint => endpoint.ProduceTo("topic2").SerializeAsJson(
                                        serializer => serializer
                                            .Configure(
                                                options =>
                                                {
                                                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                                                }))))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
            await publisher.PublishEventAsync(new TestEventThree { ContentEventThree = $"{i}" });
        }

        Host.ServiceProvider.GetRequiredService<IProducerCollection>().Count.ShouldBe(2);
        Helper.GetMessages("topic1").GetContentAsString().ShouldBe(
            [
                "{\"ContentEventOne\":\"1\"}",
                "{\"ContentEventOne\":\"2\"}",
                "{\"ContentEventOne\":\"3\"}",
                "{\"ContentEventOne\":\"4\"}",
                "{\"ContentEventOne\":\"5\"}"
            ],
            ignoreOrder: true);
        Helper.GetMessages("topic2").GetContentAsString().ShouldBe(
            [
                "{\"contentEventTwo\":\"1\"}",
                "{\"contentEventTwo\":\"2\"}",
                "{\"contentEventTwo\":\"3\"}",
                "{\"contentEventTwo\":\"4\"}",
                "{\"contentEventTwo\":\"5\"}"
            ],
            ignoreOrder: true);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldSerializeAndProduce_WhenBroadcastingToMultipleTopics()
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
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic2")))
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic3")))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        Helper.GetMessages("topic1").GetContentAsString().ShouldBe(
            [
                "{\"ContentEventOne\":\"1\"}",
                "{\"ContentEventOne\":\"2\"}",
                "{\"ContentEventOne\":\"3\"}",
                "{\"ContentEventOne\":\"4\"}",
                "{\"ContentEventOne\":\"5\"}"
            ],
            ignoreOrder: true);
        Helper.GetMessages("topic2").GetContentAsString().ShouldBe(
            [
                "{\"ContentEventOne\":\"1\"}",
                "{\"ContentEventOne\":\"2\"}",
                "{\"ContentEventOne\":\"3\"}",
                "{\"ContentEventOne\":\"4\"}",
                "{\"ContentEventOne\":\"5\"}"
            ],
            ignoreOrder: true);
        Helper.GetMessages("topic3").GetContentAsString().ShouldBe(
            [
                "{\"ContentEventOne\":\"1\"}",
                "{\"ContentEventOne\":\"2\"}",
                "{\"ContentEventOne\":\"3\"}",
                "{\"ContentEventOne\":\"4\"}",
                "{\"ContentEventOne\":\"5\"}"
            ],
            ignoreOrder: true);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduceCustomHeaders()
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
                                .Produce<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .AddHeader<TestEventWithHeaders>("x-content", envelope => envelope.Message?.Content)
                                        .AddHeader<TestEventOne>("x-content-nope", envelope => envelope.Message?.ContentEventOne)
                                        .AddHeader("x-static", 42)))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(
            new TestEventWithHeaders
            {
                Content = "Hello E2E!",
                CustomHeader = "Hello header!",
                CustomHeader2 = false
            });

        MqttApplicationMessage message = GetDefaultTopicMessages().Single();
        message.GetContentAsString().ShouldBe("{\"Content\":\"Hello E2E!\"}");

        List<MqttUserProperty> userProperties = message.UserProperties;
        userProperties.ShouldContain(property => property.Name == "x-content" && property.Value == "Hello E2E!");
        userProperties.ShouldContain(property => property.Name == "x-static" && property.Value == "42");
        userProperties.ShouldContain(property => property.Name == "x-custom-header" && property.Value == "Hello header!");
        userProperties.ShouldContain(property => property.Name == "x-custom-header2" && property.Value == "False");
        userProperties.ShouldNotContain(property => property.Name == "x-content-nope");
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldNotSetupRouteButStillWork_WhenMessageTypeIsNotSpecified()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(client => client.Produce(endpoint => endpoint.ProduceTo(DefaultTopicName)))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne());

        GetDefaultTopicMessages().Count.ShouldBe(0);

        IProducer producer = Host.ScopedServiceProvider.GetRequiredService<IProducerCollection>().GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        GetDefaultTopicMessages().Count.ShouldBe(1);
    }
}
