// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Logging;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaEndpointsConfigurationBuilderTests
{
    [Fact]
    public async Task AddInbound_WithoutMessageType_DefaultSerializerSet()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(broker => broker.AddKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(
                            clientConfiguration => clientConfiguration
                                .WithBootstrapServers("PLAINTEXT://unittest"))
                        .AddInbound(
                            consumer => consumer
                                .ConfigureClient(clientConfiguration => clientConfiguration.WithGroupId("group"))
                                .ConsumeFrom("test"))));

        KafkaBroker broker = serviceProvider.GetRequiredService<KafkaBroker>();

        await broker.ConnectAsync();

        broker.Consumers[0].Configuration.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
    }

    [Fact]
    public async Task AddInbound_WithMessageTypeGenericParameter_TypedDefaultSerializerSet()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(broker => broker.AddKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(
                            clientConfiguration => clientConfiguration
                                .WithBootstrapServers("PLAINTEXT://unittest"))
                        .AddInbound<TestEventOne>(
                            consumer => consumer
                                .ConfigureClient(clientConfiguration => clientConfiguration.WithGroupId("group"))
                                .ConsumeFrom("test"))));

        KafkaBroker broker = serviceProvider.GetRequiredService<KafkaBroker>();

        await broker.ConnectAsync();

        broker.Consumers[0].Configuration.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public async Task AddInboundAddOutbound_MultipleConfiguratorsWithInvalidEndpoints_ValidEndpointsAdded()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(broker => broker.AddKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(
                            clientConfiguration => clientConfiguration
                                .WithBootstrapServers("PLAINTEXT://unittest"))
                        .AddOutbound<TestEventOne>(
                            endpoint => endpoint
                                .ProduceTo("test1"))
                        .AddInbound(
                            consumer => consumer
                                .ConfigureClient(clientConfiguration => clientConfiguration.WithGroupId("group1"))
                                .ConsumeFrom(string.Empty))
                        .AddInbound(
                            consumer => consumer
                                .ConfigureClient(clientConfiguration => clientConfiguration.WithGroupId("group2"))
                                .ConsumeFrom("test1")))
                .AddKafkaEndpoints(_ => throw new InvalidOperationException())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(
                            clientConfiguration => clientConfiguration
                                .WithBootstrapServers("PLAINTEXT://unittest"))
                        .AddOutbound<TestEventOne>(endpoint => endpoint.ProduceTo(string.Empty))
                        .AddOutbound<TestEventOne>(endpoint => endpoint.ProduceTo("test2"))
                        .AddInbound(
                            consumer => consumer
                                .ConfigureClient(clientConfiguration => clientConfiguration.WithGroupId("group1"))
                                .ConsumeFrom("test2"))));

        KafkaBroker broker = serviceProvider.GetRequiredService<KafkaBroker>();
        await broker.ConnectAsync();

        broker.Producers.Should().HaveCount(2);
        broker.Producers[0].Configuration.RawName.Should().Be("test1");
        broker.Producers[1].Configuration.RawName.Should().Be("test2");
        broker.Consumers.Should().HaveCount(2);
        broker.Consumers[0].Configuration.RawName.Should().Be("test1");
        broker.Consumers[1].Configuration.RawName.Should().Be("test2");
    }
}
