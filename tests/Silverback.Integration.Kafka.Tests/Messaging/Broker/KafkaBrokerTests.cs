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
    public class KafkaBrokerTests
    {
        private readonly KafkaBroker _broker = new KafkaBroker(
            new MessageKeyProvider(new [] { new DefaultPropertiesMessageKeyProvider()}), 
            NullLoggerFactory.Instance, new MessageLogger());

        [Fact]
        public void GetProducer_SomeEndpoint_ProducerIsReturned()
        {
            var producer = _broker.GetProducer(new KafkaProducerEndpoint("test-endpoint"));

            producer.Should().NotBeNull();
        }

        [Fact]
        public void GetProducer_SameEndpoint_SameInstanceIsReturned()
        {
            var producer = _broker.GetProducer(new KafkaProducerEndpoint("test-endpoint"));
            var producer2 = _broker.GetProducer(new KafkaProducerEndpoint("test-endpoint"));

            producer2.Should().BeSameAs(producer);
        }

        [Fact]
        public void GetProducer_SameEndpointConfiguration_SameInstanceIsReturned()
        {
            var producer = _broker.GetProducer(new KafkaProducerEndpoint("test-endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://whatever:1111",
                    MessageTimeoutMs = 2000
                }
            });
            var producer2 = _broker.GetProducer(new KafkaProducerEndpoint("test-endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://whatever:1111",
                    MessageTimeoutMs = 2000
                }
            });

            producer2.Should().BeSameAs(producer);
        }

        [Fact]
        public void GetProducer_DifferentEndpoint_DifferentInstanceIsReturned()
        {
            var producer = _broker.GetProducer(new KafkaProducerEndpoint("test-endpoint"));
            var producer2 = _broker.GetProducer(new KafkaProducerEndpoint("other-endpoint"));

            producer2.Should().NotBeSameAs(producer);
        }

        [Fact]
        public void GetProducer_DifferentEndpointConfiguration_DifferentInstanceIsReturned()
        {
            var producer = _broker.GetProducer(new KafkaProducerEndpoint("test-endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://whatever:1111",
                    MessageTimeoutMs = 2010
                }
            });
            var producer2 = _broker.GetProducer(new KafkaProducerEndpoint("test-endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://whatever:1111",
                    MessageTimeoutMs = 2000
                }
            });
            producer2.Should().NotBeSameAs(producer);
        }
        [Fact]
        public void GetConsumer_SomeEndpoint_ConsumerIsReturned()
        {
            var consumer = _broker.GetConsumer(new KafkaConsumerEndpoint("test-endpoint"));

            consumer.Should().NotBeNull();
        }

        [Fact]
        public void GetConsumer_SameEndpoint_DifferentInstanceIsReturned()
        {
            var consumer = _broker.GetConsumer(new KafkaConsumerEndpoint("test-endpoint"));
            var consumer2 = _broker.GetConsumer(new KafkaConsumerEndpoint("other-endpoint"));

            consumer2.Should().NotBeSameAs(consumer);
        }

        [Fact]
        public void GetConsumer_DifferentEndpoint_DifferentInstanceIsReturned()
        {
            var consumer = _broker.GetConsumer(new KafkaConsumerEndpoint("test-endpoint"));
            var consumer2 = _broker.GetConsumer(new KafkaConsumerEndpoint("other-endpoint"));

            consumer2.Should().NotBeSameAs(consumer);
        }
    }
}
