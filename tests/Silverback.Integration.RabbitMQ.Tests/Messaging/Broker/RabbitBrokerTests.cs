// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Rabbit;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.RabbitMQ.Messaging.Broker
{
    public sealed class RabbitBrokerTests : IDisposable
    {
        private readonly RabbitBroker _broker;

        public RabbitBrokerTests()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddRabbit()));

            _broker = serviceProvider.GetRequiredService<RabbitBroker>();
        }

        [Fact]
        public void GetProducer_ExchangeEndpoint_ProducerIsReturned()
        {
            var producer = _broker.GetProducer(
                new RabbitExchangeProducerEndpoint("test-endpoint")
                {
                    Exchange = new RabbitExchangeConfig { ExchangeType = ExchangeType.Fanout }
                });

            producer.Should().NotBeNull();
        }

        [Fact]
        public void GetProducer_QueueEndpoint_ProducerIsReturned()
        {
            var producer = _broker.GetProducer(new RabbitQueueProducerEndpoint("test-endpoint"));

            producer.Should().NotBeNull();
        }

        [Fact]
        public void GetProducer_ExchangeEndpoint_SameInstanceIsReturned()
        {
            var producer = _broker.GetProducer(
                new RabbitExchangeProducerEndpoint("test-endpoint")
                {
                    Exchange = new RabbitExchangeConfig { ExchangeType = ExchangeType.Fanout }
                });
            var producer2 = _broker.GetProducer(
                new RabbitExchangeProducerEndpoint("test-endpoint")
                {
                    Exchange = new RabbitExchangeConfig { ExchangeType = ExchangeType.Fanout }
                });

            producer2.Should().BeSameAs(producer);
        }

        [Fact]
        public void GetProducer_QueueEndpoint_SameInstanceIsReturned()
        {
            var producer = _broker.GetProducer(new RabbitQueueProducerEndpoint("test-endpoint"));
            var producer2 = _broker.GetProducer(new RabbitQueueProducerEndpoint("test-endpoint"));

            producer2.Should().BeSameAs(producer);
        }

        [Fact]
        public void GetProducer_SameExchangeEndpointConfiguration_SameInstanceIsReturned()
        {
            var producer = _broker.GetProducer(
                new RabbitExchangeProducerEndpoint("test-endpoint")
                {
                    Exchange = new RabbitExchangeConfig
                    {
                        ExchangeType = ExchangeType.Direct,
                        IsAutoDeleteEnabled = false,
                        IsDurable = true
                    }
                });
            var producer2 = _broker.GetProducer(
                new RabbitExchangeProducerEndpoint("test-endpoint")
                {
                    Exchange = new RabbitExchangeConfig
                    {
                        ExchangeType = ExchangeType.Direct,
                        IsAutoDeleteEnabled = false,
                        IsDurable = true
                    }
                });

            producer2.Should().BeSameAs(producer);
        }

        [Fact]
        public void GetProducer_SameQueueEndpointConfiguration_SameInstanceIsReturned()
        {
            var producer = _broker.GetProducer(
                new RabbitQueueProducerEndpoint("test-endpoint")
                {
                    Queue = new RabbitQueueConfig
                    {
                        IsAutoDeleteEnabled = false,
                        IsDurable = true
                    }
                });
            var producer2 = _broker.GetProducer(
                new RabbitQueueProducerEndpoint("test-endpoint")
                {
                    Queue = new RabbitQueueConfig
                    {
                        IsAutoDeleteEnabled = false,
                        IsDurable = true
                    }
                });

            producer2.Should().BeSameAs(producer);
        }

        [Fact]
        public void GetProducer_DifferentExchangeEndpoint_DifferentInstanceIsReturned()
        {
            var producer = _broker.GetProducer(
                new RabbitExchangeProducerEndpoint("test-endpoint")
                {
                    Exchange = new RabbitExchangeConfig { ExchangeType = ExchangeType.Fanout }
                });
            var producer2 = _broker.GetProducer(
                new RabbitExchangeProducerEndpoint("other-endpoint")
                {
                    Exchange = new RabbitExchangeConfig { ExchangeType = ExchangeType.Fanout }
                });

            producer2.Should().NotBeSameAs(producer);
        }

        [Fact]
        public void GetProducer_DifferentQueueEndpoint_DifferentInstanceIsReturned()
        {
            var producer = _broker.GetProducer(new RabbitQueueProducerEndpoint("test-endpoint"));
            var producer2 = _broker.GetProducer(new RabbitQueueProducerEndpoint("other-endpoint"));

            producer2.Should().NotBeSameAs(producer);
        }

        [Fact]
        public void GetProducer_DifferentEndpointType_DifferentInstanceIsReturned()
        {
            var producer = _broker.GetProducer(new RabbitQueueProducerEndpoint("test-endpoint"));
            var producer2 = _broker.GetProducer(
                new RabbitExchangeProducerEndpoint("other-endpoint")
                {
                    Exchange = new RabbitExchangeConfig { ExchangeType = ExchangeType.Fanout }
                });

            producer2.Should().NotBeSameAs(producer);
        }

        [Fact]
        public void GetProducer_DifferentExchangeEndpointConfiguration_DifferentInstanceIsReturned()
        {
            var producer = _broker.GetProducer(
                new RabbitExchangeProducerEndpoint("test-endpoint")
                {
                    Exchange = new RabbitExchangeConfig
                    {
                        ExchangeType = ExchangeType.Direct,
                        IsAutoDeleteEnabled = false,
                        IsDurable = true
                    }
                });
            var producer2 = _broker.GetProducer(
                new RabbitExchangeProducerEndpoint("test-endpoint")
                {
                    Exchange = new RabbitExchangeConfig
                    {
                        ExchangeType = ExchangeType.Direct,
                        IsAutoDeleteEnabled = true,
                        IsDurable = false
                    }
                });

            producer2.Should().NotBeSameAs(producer);
        }

        [Fact]
        public void GetProducer_DifferentQueueEndpointConfiguration_DifferentInstanceIsReturned()
        {
            var producer = _broker.GetProducer(
                new RabbitQueueProducerEndpoint("test-endpoint")
                {
                    Queue = new RabbitQueueConfig
                    {
                        IsAutoDeleteEnabled = false,
                        IsDurable = true
                    }
                });
            var producer2 = _broker.GetProducer(
                new RabbitQueueProducerEndpoint("test-endpoint")
                {
                    Queue = new RabbitQueueConfig
                    {
                        IsAutoDeleteEnabled = false,
                        IsDurable = true,
                        IsExclusive = true
                    }
                });

            producer2.Should().NotBeSameAs(producer);
        }

        [Fact]
        public void AddConsumer_ExchangeEndpoint_ConsumerIsReturned()
        {
            var consumer = _broker.AddConsumer(
                new RabbitExchangeConsumerEndpoint("test-endpoint")
                {
                    Exchange = new RabbitExchangeConfig { ExchangeType = ExchangeType.Fanout }
                });

            consumer.Should().NotBeNull();
        }

        [Fact]
        public void AddConsumer_QueueEndpoint_ConsumerIsReturned()
        {
            var consumer = _broker.AddConsumer(new RabbitQueueConsumerEndpoint("test-endpoint"));

            consumer.Should().NotBeNull();
        }

        [Fact]
        public void AddConsumer_SameExchangeEndpoint_DifferentInstanceIsReturned()
        {
            var consumer = _broker.AddConsumer(
                new RabbitExchangeConsumerEndpoint("test-endpoint")
                {
                    Exchange = new RabbitExchangeConfig { ExchangeType = ExchangeType.Fanout }
                });
            var consumer2 = _broker.AddConsumer(
                new RabbitExchangeConsumerEndpoint("test-endpoint")
                {
                    Exchange = new RabbitExchangeConfig { ExchangeType = ExchangeType.Fanout }
                });

            consumer2.Should().NotBeSameAs(consumer);
        }

        [Fact]
        public void AddConsumer_SameQueueEndpoint_DifferentInstanceIsReturned()
        {
            var consumer = _broker.AddConsumer(new RabbitQueueConsumerEndpoint("test-endpoint"));
            var consumer2 = _broker.AddConsumer(new RabbitQueueConsumerEndpoint("test-endpoint"));

            consumer2.Should().NotBeSameAs(consumer);
        }

        [Fact]
        public void AddConsumer_DifferentExchangeEndpoint_DifferentInstanceIsReturned()
        {
            var consumer = _broker.AddConsumer(
                new RabbitExchangeConsumerEndpoint("test-endpoint")
                {
                    Exchange = new RabbitExchangeConfig { ExchangeType = ExchangeType.Fanout }
                });
            var consumer2 = _broker.AddConsumer(
                new RabbitExchangeConsumerEndpoint("other-endpoint")
                {
                    Exchange = new RabbitExchangeConfig { ExchangeType = ExchangeType.Fanout }
                });

            consumer2.Should().NotBeSameAs(consumer);
        }

        [Fact]
        public void AddConsumer_DifferentQueueEndpoint_DifferentInstanceIsReturned()
        {
            var consumer = _broker.AddConsumer(new RabbitQueueConsumerEndpoint("test-endpoint"));
            var consumer2 = _broker.AddConsumer(new RabbitQueueConsumerEndpoint("other-endpoint"));

            consumer2.Should().NotBeSameAs(consumer);
        }

        [Fact]
        public void AddConsumer_DifferentEndpointType_DifferentInstanceIsReturned()
        {
            var consumer = _broker.AddConsumer(
                new RabbitExchangeConsumerEndpoint("test-endpoint")
                {
                    Exchange = new RabbitExchangeConfig { ExchangeType = ExchangeType.Fanout }
                });
            var consumer2 = _broker.AddConsumer(new RabbitQueueConsumerEndpoint("test-endpoint"));

            consumer2.Should().NotBeSameAs(consumer);
        }

        public void Dispose()
        {
            _broker.Dispose();
        }
    }
}
