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
    public sealed class KafkaBrokerTests : IDisposable
    {
        private readonly KafkaBroker _broker;

        public KafkaBrokerTests()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddKafka()));

            _broker = serviceProvider.GetRequiredService<KafkaBroker>();
        }

        [Fact]
        public void GetProducer_SomeEndpoint_ProducerIsReturned()
        {
            var producer = _broker.GetProducer(
                new KafkaProducerEndpoint("test-endpoint")
                {
                    Configuration =
                    {
                        BootstrapServers = "PLAINTEXT://whatever:1111"
                    }
                });

            producer.Should().NotBeNull();
        }

        [Fact]
        public void GetProducer_SameEndpoint_SameInstanceIsReturned()
        {
            var producer = _broker.GetProducer(
                new KafkaProducerEndpoint("test-endpoint")
                {
                    Configuration =
                    {
                        BootstrapServers = "PLAINTEXT://whatever:1111"
                    }
                });

            var producer2 = _broker.GetProducer(
                new KafkaProducerEndpoint("test-endpoint")
                {
                    Configuration =
                    {
                        BootstrapServers = "PLAINTEXT://whatever:1111"
                    }
                });

            producer2.Should().BeSameAs(producer);
        }

        [Fact]
        public void GetProducer_SameEndpointConfiguration_SameInstanceIsReturned()
        {
            var producer = _broker.GetProducer(
                new KafkaProducerEndpoint("test-endpoint")
                {
                    Configuration =
                    {
                        BootstrapServers = "PLAINTEXT://whatever:1111",
                        MessageTimeoutMs = 2000
                    }
                });
            var producer2 = _broker.GetProducer(
                new KafkaProducerEndpoint("test-endpoint")
                {
                    Configuration =
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
            var producer = _broker.GetProducer(
                new KafkaProducerEndpoint("test-endpoint")
                {
                    Configuration =
                    {
                        BootstrapServers = "PLAINTEXT://whatever:1111"
                    }
                });

            var producer2 = _broker.GetProducer(
                new KafkaProducerEndpoint("other-endpoint")
                {
                    Configuration =
                    {
                        BootstrapServers = "PLAINTEXT://whatever:1111"
                    }
                });

            producer2.Should().NotBeSameAs(producer);
        }

        [Fact]
        public void GetProducer_DifferentEndpointConfiguration_DifferentInstanceIsReturned()
        {
            var producer = _broker.GetProducer(
                new KafkaProducerEndpoint("test-endpoint")
                {
                    Configuration =
                    {
                        BootstrapServers = "PLAINTEXT://whatever:1111",
                        MessageTimeoutMs = 2010
                    }
                });
            var producer2 = _broker.GetProducer(
                new KafkaProducerEndpoint("test-endpoint")
                {
                    Configuration =
                    {
                        BootstrapServers = "PLAINTEXT://whatever:1111",
                        MessageTimeoutMs = 2000
                    }
                });
            producer2.Should().NotBeSameAs(producer);
        }

        [Fact]
        public void AddConsumer_SomeEndpoint_ConsumerIsReturned()
        {
            var consumer = _broker.AddConsumer(
                new KafkaConsumerEndpoint("test-endpoint")
                {
                    Configuration =
                    {
                        BootstrapServers = "PLAINTEXT://whatever:1111"
                    }
                });

            consumer.Should().NotBeNull();
        }

        [Fact]
        public void AddConsumer_SameEndpoint_DifferentInstanceIsReturned()
        {
            var consumer = _broker.AddConsumer(
                new KafkaConsumerEndpoint("test-endpoint")
                {
                    Configuration =
                    {
                        BootstrapServers = "PLAINTEXT://whatever:1111"
                    }
                });

            var consumer2 = _broker.AddConsumer(
                new KafkaConsumerEndpoint("test-endpoint")
                {
                    Configuration =
                    {
                        BootstrapServers = "PLAINTEXT://whatever:1111"
                    }
                });

            consumer2.Should().NotBeSameAs(consumer);
        }

        [Fact]
        public void AddConsumer_DifferentEndpoint_DifferentInstanceIsReturned()
        {
            var consumer = _broker.AddConsumer(
                new KafkaConsumerEndpoint("test-endpoint")
                {
                    Configuration =
                    {
                        BootstrapServers = "PLAINTEXT://whatever:1111"
                    }
                });

            var consumer2 = _broker.AddConsumer(
                new KafkaConsumerEndpoint("other-endpoint")
                {
                    Configuration =
                    {
                        BootstrapServers = "PLAINTEXT://whatever:1111"
                    }
                });

            consumer2.Should().NotBeSameAs(consumer);
        }

        public void Dispose() => _broker?.Dispose();
    }
}
