// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker
{
    public class KafkaProducerTests
    {
        private readonly KafkaBroker _broker = new KafkaBroker(
            new MessageKeyProvider(new[] { new DefaultPropertiesMessageKeyProvider() }),
            NullLoggerFactory.Instance, new MessageLogger());

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
    }
}