// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
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

        Helper.GetMessages("topic1").Should().HaveCount(1);
        Helper.GetMessages("topic2").Should().HaveCount(1);
        Helper.GetMessages("topic3").Should().HaveCount(1);

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        Helper.GetMessages("topic1").Should().HaveCount(2);
        Helper.GetMessages("topic2").Should().HaveCount(1);
        Helper.GetMessages("topic3").Should().HaveCount(2);
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

        Helper.GetMessages("topic1").Should().HaveCount(1);
        Helper.GetMessages("topic2").Should().HaveCount(1);
        Helper.GetMessages("topic3").Should().HaveCount(1);

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        Helper.GetMessages("topic1").Should().HaveCount(2);
        Helper.GetMessages("topic2").Should().HaveCount(1);
        Helper.GetMessages("topic3").Should().HaveCount(2);
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

        Helper.GetMessages("topic1").Should().HaveCount(1);
        Helper.GetMessages("topic2").Should().HaveCount(1);
        Helper.GetMessages("topic3").Should().HaveCount(1);

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        Helper.GetMessages("topic1").Should().HaveCount(2);
        Helper.GetMessages("topic2").Should().HaveCount(1);
        Helper.GetMessages("topic3").Should().HaveCount(2);
    }

    private sealed class TestEndpointResolver : IMqttProducerEndpointResolver<TestEventOne>
    {
        public string GetTopic(TestEventOne? message) =>
            message?.ContentEventOne switch
            {
                "1" => "topic1",
                "2" => "topic2",
                "3" => "topic3",
                _ => throw new InvalidOperationException()
            };
    }
}
