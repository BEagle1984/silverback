// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker
{
    public sealed class KafkaBrokerTests : IDisposable
    {
        private static readonly MessagesReceivedAsyncCallback VoidCallback = args => Task.CompletedTask;

        private readonly KafkaBroker _broker;

        public KafkaBrokerTests()
        {
            var services = new ServiceCollection()
                .AddSingleton<KafkaEventsHandler>()
                .AddSingleton(typeof(ISilverbackIntegrationLogger<>), typeof(IntegrationLoggerSubstitute<>));

            _broker = new KafkaBroker(
                Enumerable.Empty<IBrokerBehavior>(),
                services.BuildServiceProvider());
        }

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
            var producer = _broker.GetProducer(
                new KafkaProducerEndpoint("test-endpoint")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://whatever:1111",
                        MessageTimeoutMs = 2000
                    }
                });
            var producer2 = _broker.GetProducer(
                new KafkaProducerEndpoint("test-endpoint")
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
            var producer = _broker.GetProducer(
                new KafkaProducerEndpoint("test-endpoint")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://whatever:1111",
                        MessageTimeoutMs = 2010
                    }
                });
            var producer2 = _broker.GetProducer(
                new KafkaProducerEndpoint("test-endpoint")
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
        public void AddConsumer_SomeEndpoint_ConsumerIsReturned()
        {
            var consumer = _broker.AddConsumer(new KafkaConsumerEndpoint("test-endpoint"), VoidCallback);

            consumer.Should().NotBeNull();
        }

        [Fact]
        public void AddConsumer_SameEndpoint_DifferentInstanceIsReturned()
        {
            var consumer = _broker.AddConsumer(new KafkaConsumerEndpoint("test-endpoint"), VoidCallback);
            var consumer2 = _broker.AddConsumer(new KafkaConsumerEndpoint("test-endpoint"), VoidCallback);

            consumer2.Should().NotBeSameAs(consumer);
        }

        [Fact]
        public void AddConsumer_DifferentEndpoint_DifferentInstanceIsReturned()
        {
            var consumer = _broker.AddConsumer(new KafkaConsumerEndpoint("test-endpoint"), VoidCallback);
            var consumer2 = _broker.AddConsumer(new KafkaConsumerEndpoint("other-endpoint"), VoidCallback);

            consumer2.Should().NotBeSameAs(consumer);
        }

        public void Dispose() => _broker?.Dispose();
    }
}
