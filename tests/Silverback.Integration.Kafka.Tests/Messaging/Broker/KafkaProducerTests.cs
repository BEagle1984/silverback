// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker
{
    public sealed class KafkaProducerTests : IDisposable
    {
        private readonly KafkaBroker _broker;

        public KafkaProducerTests()
        {
            var services = new ServiceCollection()
                .AddSingleton<EndpointsConfiguratorsInvoker>()
                .AddSingleton(typeof(ISilverbackIntegrationLogger<>), typeof(IntegrationLoggerSubstitute<>));

            _broker = new KafkaBroker(
                Enumerable.Empty<IBrokerBehavior>(),
                services.BuildServiceProvider());
        }

        [Fact]
        public void Produce_SomeMessage_EndpointConfigurationIsNotAltered()
        {
            var endpoint = new KafkaProducerEndpoint("test-endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://whatever:1111",
                    MessageTimeoutMs = 10
                }
            };
            var endpointCopy = new KafkaProducerEndpoint("test-endpoint")
            {
                Configuration = new KafkaProducerConfig
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
