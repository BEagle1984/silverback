// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker
{
    public sealed class KafkaProducerTests : IDisposable
    {
        private readonly KafkaBroker _broker;

        public KafkaProducerTests()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<KafkaBroker>()));

            _broker = serviceProvider.GetRequiredService<KafkaBroker>();
        }

        [Fact]
        public void Produce_SomeMessage_EndpointConfigurationIsNotAltered()
        {
            var endpoint = new KafkaProducerEndpoint("test-endpoint")
            {
                Configuration =
                {
                    BootstrapServers = "PLAINTEXT://whatever:1111",
                    MessageTimeoutMs = 10
                }
            };
            var endpointCopy = new KafkaProducerEndpoint("test-endpoint")
            {
                Configuration =
                {
                    BootstrapServers = "PLAINTEXT://whatever:1111",
                    MessageTimeoutMs = 10
                }
            };

            try
            {
                _broker.GetProducer(endpoint).Produce("test");
            }
            catch
            {
                // Swallow, we don't care...
            }

            endpoint.Should().BeEquivalentTo(endpointCopy);
        }

        public void Dispose() => _broker?.Dispose();
    }
}
