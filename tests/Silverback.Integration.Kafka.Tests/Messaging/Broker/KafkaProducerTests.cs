// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker
{
    public sealed class KafkaProducerTests : IDisposable
    {
        private readonly KafkaBroker _broker = new KafkaBroker(
            Enumerable.Empty<IBrokerBehavior>(),
            NullLoggerFactory.Instance,
            Substitute.For<IServiceProvider>());

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
