// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ProducerEndpointFixture
{
    [Fact]
    public async Task ProducerEndpoint_ShouldProduce_WhenTopicFunctionIsSet()
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
                                .Produce<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo(
                                            message => message?.ContentEventOne switch
                                            {
                                                "1" => "topic1",
                                                "2" => "topic2",
                                                "3" => "topic3",
                                                _ => throw new InvalidOperationException()
                                            })))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        Helper.GetMessages("topic1").Count.ShouldBe(1);
        Helper.GetMessages("topic2").Count.ShouldBe(1);
        Helper.GetMessages("topic3").Count.ShouldBe(1);

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        Helper.GetMessages("topic1").Count.ShouldBe(2);
        Helper.GetMessages("topic2").Count.ShouldBe(1);
        Helper.GetMessages("topic3").Count.ShouldBe(2);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduce_WhenTopicFormatIsSet()
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
                                .Produce<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo(
                                            "topic{0}",
                                            message => message?.ContentEventOne switch
                                            {
                                                "1" => ["1"],
                                                "2" => ["2"],
                                                "3" => ["3"],
                                                _ => throw new InvalidOperationException()
                                            })))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        Helper.GetMessages("topic1").Count.ShouldBe(1);
        Helper.GetMessages("topic2").Count.ShouldBe(1);
        Helper.GetMessages("topic3").Count.ShouldBe(1);

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        Helper.GetMessages("topic1").Count.ShouldBe(2);
        Helper.GetMessages("topic2").Count.ShouldBe(1);
        Helper.GetMessages("topic3").Count.ShouldBe(2);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduce_WhenCustomEndpointResolverIsSet()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSingleton<TestEndpointResolver>()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Produce<TestEventOne>(endpoint => endpoint.UseEndpointResolver<TestEndpointResolver>()))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        Helper.GetMessages("topic1").Count.ShouldBe(1);
        Helper.GetMessages("topic2").Count.ShouldBe(1);
        Helper.GetMessages("topic3").Count.ShouldBe(1);

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        Helper.GetMessages("topic1").Count.ShouldBe(2);
        Helper.GetMessages("topic2").Count.ShouldBe(1);
        Helper.GetMessages("topic3").Count.ShouldBe(2);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduceToDynamicEndpointSetViaEnvelopeExtensions()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(client => client.Produce<IIntegrationEvent>(endpoint => endpoint.ProduceToDynamicTopic()))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.WrapAndPublishAsync(
            new TestEventOne(),
            envelope => envelope.SetMqttDestinationTopic("topic1"));
        await publisher.WrapAndPublishBatchAsync(
            new IIntegrationEvent?[]
            {
                new TestEventOne(),
                null,
                new TestEventTwo(),
                new TestEventOne()
            },
            envelope => envelope.SetMqttDestinationTopic(envelope.MessageType == typeof(TestEventOne) ? "topic1" : "topic2"));

        Helper.GetMessages("topic1").Count.ShouldBe(3);
        Helper.GetMessages("topic2").Count.ShouldBe(2);
    }

    private sealed class TestEndpointResolver : IMqttProducerEndpointResolver<TestEventOne>
    {
        public string GetTopic(IOutboundEnvelope<TestEventOne> envelope) =>
            envelope.Message?.ContentEventOne switch
            {
                "1" => "topic1",
                "2" => "topic2",
                "3" => "topic3",
                _ => throw new InvalidOperationException()
            };
    }
}
