// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Packets;
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
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        IReadOnlyList<MqttApplicationMessage> messages = GetDefaultTopicMessages();
        messages.Should().HaveCount(1);
        messages[0].Payload.Should().BeEquivalentTo(rawMessage);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldSerializeAndProduce_WhenMultipleClientsForDifferentMessagesAreConfigured()
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

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
            await publisher.PublishAsync(new TestEventThree { ContentEventThree = $"{i}" });
        }

        Host.ServiceProvider.GetRequiredService<IProducerCollection>().Should().HaveCount(2);
        Helper.GetMessages("topic1").GetContentAsString().Should().BeEquivalentTo(
            new[]
            {
                "{\"ContentEventOne\":\"1\"}",
                "{\"ContentEventOne\":\"2\"}",
                "{\"ContentEventOne\":\"3\"}",
                "{\"ContentEventOne\":\"4\"}",
                "{\"ContentEventOne\":\"5\"}"
            },
            options => options.WithoutStrictOrdering());
        Helper.GetMessages("topic2").GetContentAsString().Should().BeEquivalentTo(
            new[]
            {
                "{\"contentEventTwo\":\"1\"}",
                "{\"contentEventTwo\":\"2\"}",
                "{\"contentEventTwo\":\"3\"}",
                "{\"contentEventTwo\":\"4\"}",
                "{\"contentEventTwo\":\"5\"}"
            },
            options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldSerializeAndProduce_WhenSingleClientHandlesMultipleTypes()
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

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
            await publisher.PublishAsync(new TestEventThree { ContentEventThree = $"{i}" });
        }

        Host.ServiceProvider.GetRequiredService<IProducerCollection>().Should().HaveCount(2);
        Helper.GetMessages("topic1").GetContentAsString().Should().BeEquivalentTo(
            new[]
            {
                "{\"ContentEventOne\":\"1\"}",
                "{\"ContentEventOne\":\"2\"}",
                "{\"ContentEventOne\":\"3\"}",
                "{\"ContentEventOne\":\"4\"}",
                "{\"ContentEventOne\":\"5\"}"
            },
            options => options.WithoutStrictOrdering());
        Helper.GetMessages("topic2").GetContentAsString().Should().BeEquivalentTo(
            new[]
            {
                "{\"contentEventTwo\":\"1\"}",
                "{\"contentEventTwo\":\"2\"}",
                "{\"contentEventTwo\":\"3\"}",
                "{\"contentEventTwo\":\"4\"}",
                "{\"contentEventTwo\":\"5\"}"
            },
            options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldSerializeAndProduce_WhenBroadcastingToMultipleTopics()
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
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic2")))
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic3")))));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        Helper.GetMessages("topic1").GetContentAsString().Should().BeEquivalentTo(
            new[]
            {
                "{\"ContentEventOne\":\"1\"}",
                "{\"ContentEventOne\":\"2\"}",
                "{\"ContentEventOne\":\"3\"}",
                "{\"ContentEventOne\":\"4\"}",
                "{\"ContentEventOne\":\"5\"}"
            },
            options => options.WithoutStrictOrdering());
        Helper.GetMessages("topic2").GetContentAsString().Should().BeEquivalentTo(
            new[]
            {
                "{\"ContentEventOne\":\"1\"}",
                "{\"ContentEventOne\":\"2\"}",
                "{\"ContentEventOne\":\"3\"}",
                "{\"ContentEventOne\":\"4\"}",
                "{\"ContentEventOne\":\"5\"}"
            },
            options => options.WithoutStrictOrdering());
        Helper.GetMessages("topic3").GetContentAsString().Should().BeEquivalentTo(
            new[]
            {
                "{\"ContentEventOne\":\"1\"}",
                "{\"ContentEventOne\":\"2\"}",
                "{\"ContentEventOne\":\"3\"}",
                "{\"ContentEventOne\":\"4\"}",
                "{\"ContentEventOne\":\"5\"}"
            },
            options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduceCustomHeaders()
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
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Produce<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .AddHeader<TestEventWithHeaders>("x-content", envelope => envelope.Message?.Content)
                                        .AddHeader<TestEventOne>("x-content-nope", envelope => envelope.Message?.ContentEventOne)
                                        .AddHeader("x-static", 42)))));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(
            new TestEventWithHeaders
            {
                Content = "Hello E2E!",
                CustomHeader = "Hello header!",
                CustomHeader2 = false
            });

        MqttApplicationMessage message = GetDefaultTopicMessages().Single();
        message.GetContentAsString().Should().BeEquivalentTo("{\"Content\":\"Hello E2E!\"}");

        List<MqttUserProperty> userProperties = message.UserProperties;
        userProperties.Should().ContainEquivalentOf(new MqttUserProperty("x-content", "Hello E2E!"));
        userProperties.Should().ContainEquivalentOf(new MqttUserProperty("x-static", "42"));
        userProperties.Should().ContainEquivalentOf(new MqttUserProperty("x-custom-header", "Hello header!"));
        userProperties.Should().ContainEquivalentOf(new MqttUserProperty("x-custom-header2", "False"));
        userProperties.Select(userProperty => userProperty.Name).Should().NotContain("x-content-nope");
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldNotSetupRouteButStillWork_WhenMessageTypeIsNotSpecified()
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
                        .AddClient(client => client.Produce(endpoint => endpoint.ProduceTo(DefaultTopicName)))));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne());

        GetDefaultTopicMessages().Should().HaveCount(0);

        IProducer producer = Host.ScopedServiceProvider.GetRequiredService<IProducerCollection>().GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        GetDefaultTopicMessages().Should().HaveCount(1);
    }
}
