// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Newtonsoft.Json;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Integration.RabbitMQ.Messaging
{
    public class RabbitQueueConsumerEndpointTests
    {
        [Fact]
        public void Equals_SameEndpointInstance_TrueIsReturned()
        {
            var endpoint = new RabbitQueueConsumerEndpoint("endpoint")
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
            var endpoint1 = new RabbitQueueConsumerEndpoint("endpoint")
            {
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false,
                    IsAutoDeleteEnabled = true,
                    IsExclusive = true
                }
            };
            var endpoint2 = new RabbitQueueConsumerEndpoint("endpoint")
            {
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
            var endpoint1 = new RabbitQueueConsumerEndpoint("endpoint")
            {
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false,
                    IsAutoDeleteEnabled = true,
                    IsExclusive = true
                }
            };

            var json = JsonConvert.SerializeObject(endpoint1,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            var endpoint2 = JsonConvert.DeserializeObject<RabbitQueueConsumerEndpoint>(json,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            endpoint1.Equals(endpoint2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentName_FalseIsReturned()
        {
            var endpoint1 = new RabbitQueueConsumerEndpoint("endpoint");
            var endpoint2 = new RabbitQueueConsumerEndpoint("endpoint2");

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentConfiguration_FalseIsReturned()
        {
            var endpoint1 = new RabbitQueueConsumerEndpoint("endpoint")
            {
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false,
                    IsAutoDeleteEnabled = true,
                    IsExclusive = true
                }
            };
            var endpoint2 = new RabbitQueueConsumerEndpoint("endpoint")
            {
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
        public void SerializationAndDeserialization_NoInformationIsLost()
        {
            var endpoint1 = new RabbitQueueConsumerEndpoint("endpoint")
            {
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

            var endpoint2 = JsonConvert.DeserializeObject<RabbitQueueConsumerEndpoint>(json,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            endpoint2.Should().BeEquivalentTo(endpoint1);
        }
    }
}