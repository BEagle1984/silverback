// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Logging;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka
{
    public class KafkaEndpointsConfigurationBuilderTests
    {
        [Fact]
        public async Task AddInbound_WithoutMessageType_DefaultSerializerSet()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(broker => broker.AddKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .Configure(
                                config =>
                                {
                                    config.BootstrapServers = "PLAINTEXT://unittest";
                                })
                            .AddInbound(
                                endpoint => endpoint
                                    .Configure(
                                        config =>
                                        {
                                            config.GroupId = "group";
                                        })
                                    .ConsumeFrom("test"))));

            var broker = serviceProvider.GetRequiredService<KafkaBroker>();

            await broker.ConnectAsync();

            broker.Consumers[0].Endpoint.Serializer.Should().BeOfType<JsonMessageSerializer>();
        }

        [Fact]
        public async Task AddInbound_WithMessageTypeGenericParameter_TypedDefaultSerializerSet()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(broker => broker.AddKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .Configure(
                                config =>
                                {
                                    config.BootstrapServers = "PLAINTEXT://unittest";
                                })
                            .AddInbound<TestEventOne>(
                                endpoint => endpoint
                                    .Configure(
                                        config =>
                                        {
                                            config.GroupId = "group";
                                        })
                                    .ConsumeFrom("test"))));

            var broker = serviceProvider.GetRequiredService<KafkaBroker>();

            await broker.ConnectAsync();

            broker.Consumers[0].Endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
        }

        [Fact]
        public async Task AddInbound_WithMessageTypeParameter_TypedDefaultSerializerSet()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(broker => broker.AddKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .Configure(
                                config =>
                                {
                                    config.BootstrapServers = "PLAINTEXT://unittest";
                                })
                            .AddInbound(
                                typeof(TestEventOne),
                                endpoint => endpoint
                                    .Configure(
                                        config =>
                                        {
                                            config.GroupId = "group";
                                        })
                                    .ConsumeFrom("test"))));

            var broker = serviceProvider.GetRequiredService<KafkaBroker>();

            await broker.ConnectAsync();

            broker.Consumers[0].Endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
        }
    }
}
