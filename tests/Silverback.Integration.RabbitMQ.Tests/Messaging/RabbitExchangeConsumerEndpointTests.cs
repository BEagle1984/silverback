// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Integration.RabbitMQ.Messaging
{
    public class RabbitExchangeConsumerEndpointTests
    {
        [Fact]
        public void Equals_SameEndpointInstance_TrueIsReturned()
        {
            var endpoint = new RabbitExchangeConsumerEndpoint("endpoint")
            {
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false
                }
            };

            endpoint.Equals(endpoint).Should().BeTrue();
        }

        [Fact]
        public void Equals_SameConfiguration_TrueIsReturned()
        {
            var endpoint1 = new RabbitExchangeConsumerEndpoint("endpoint")
            {
                Exchange = new RabbitExchangeConfig
                {
                    ExchangeType = ExchangeType.Topic,
                    IsDurable = false,
                    IsAutoDeleteEnabled = true
                },
                QueueName = "queue",
                RoutingKey = "key",
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false,
                    IsAutoDeleteEnabled = true,
                    IsExclusive = true
                }
            };
            var endpoint2 = new RabbitExchangeConsumerEndpoint("endpoint")
            {
                Exchange = new RabbitExchangeConfig
                {
                    ExchangeType = ExchangeType.Topic,
                    IsDurable = false,
                    IsAutoDeleteEnabled = true
                },
                QueueName = "queue",
                RoutingKey = "key",
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false,
                    IsAutoDeleteEnabled = true,
                    IsExclusive = true
                }
            };

            endpoint1.Equals(endpoint2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DeserializedEndpoint_TrueIsReturned()
        {
            var endpoint1 = new RabbitExchangeConsumerEndpoint("endpoint")
            {
                Exchange = new RabbitExchangeConfig
                {
                    ExchangeType = ExchangeType.Topic,
                    IsDurable = false,
                    IsAutoDeleteEnabled = true
                },
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false,
                    IsAutoDeleteEnabled = true,
                    IsExclusive = true
                }
            };

            var json = JsonConvert.SerializeObject(endpoint1,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            var endpoint2 = JsonConvert.DeserializeObject<RabbitExchangeConsumerEndpoint>(json,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            endpoint1.Equals(endpoint2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentName_FalseIsReturned()
        {
            var endpoint1 = new RabbitExchangeConsumerEndpoint("endpoint");
            var endpoint2 = new RabbitExchangeConsumerEndpoint("endpoint2");

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentExchangeConfiguration_FalseIsReturned()
        {
            var endpoint1 = new RabbitExchangeConsumerEndpoint("endpoint")
            {
                Exchange = new RabbitExchangeConfig
                {
                    ExchangeType = ExchangeType.Fanout,
                    IsDurable = false,
                    IsAutoDeleteEnabled = true
                },
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false,
                    IsAutoDeleteEnabled = true,
                    IsExclusive = true
                }
            };
            var endpoint2 = new RabbitExchangeConsumerEndpoint("endpoint")
            {
                Exchange = new RabbitExchangeConfig
                {
                    ExchangeType = ExchangeType.Topic,
                    IsDurable = false,
                    IsAutoDeleteEnabled = true
                },
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false,
                    IsAutoDeleteEnabled = true,
                    IsExclusive = true
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentQueueConfiguration_FalseIsReturned()
        {
            var endpoint1 = new RabbitExchangeConsumerEndpoint("endpoint")
            {
                Exchange = new RabbitExchangeConfig
                {
                    ExchangeType = ExchangeType.Topic,
                    IsDurable = false,
                    IsAutoDeleteEnabled = true
                },
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false,
                    IsAutoDeleteEnabled = true,
                    IsExclusive = true
                }
            };
            var endpoint2 = new RabbitExchangeConsumerEndpoint("endpoint")
            {
                Exchange = new RabbitExchangeConfig
                {
                    ExchangeType = ExchangeType.Topic,
                    IsDurable = false,
                    IsAutoDeleteEnabled = true
                },
                Queue = new RabbitQueueConfig
                {
                    IsDurable = true,
                    IsAutoDeleteEnabled = false,
                    IsExclusive = false
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentQueueName_FalseIsReturned()
        {
            var endpoint1 = new RabbitExchangeConsumerEndpoint("endpoint")
            {
                QueueName = "queue1"
            };
            var endpoint2 = new RabbitExchangeConsumerEndpoint("endpoint")
            {
                QueueName = "queue2"
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentRoutingKey_FalseIsReturned()
        {
            var endpoint1 = new RabbitExchangeConsumerEndpoint("endpoint")
            {
                RoutingKey = "key"
            };
            var endpoint2 = new RabbitExchangeConsumerEndpoint("endpoint")
            {
                QueueName = "someotherkey"
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void SerializationAndDeserialization_NoInformationIsLost()
        {
            var endpoint1 = new RabbitExchangeConsumerEndpoint("endpoint")
            {
                Exchange = new RabbitExchangeConfig
                {
                    ExchangeType = ExchangeType.Topic,
                    IsDurable = false,
                    IsAutoDeleteEnabled = true
                },
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false,
                    IsAutoDeleteEnabled = true,
                    IsExclusive = true
                },
                Serializer = new JsonMessageSerializer
                {
                    Settings =
                    {
                        MaxDepth = 100
                    }
                }
            };

            var json = JsonConvert.SerializeObject(endpoint1,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            var endpoint2 = JsonConvert.DeserializeObject<RabbitExchangeConsumerEndpoint>(json,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            endpoint2.Should().BeEquivalentTo(endpoint1);
        }
    }
}