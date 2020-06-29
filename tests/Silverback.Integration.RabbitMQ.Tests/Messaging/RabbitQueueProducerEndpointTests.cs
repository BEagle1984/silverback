// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Integration.RabbitMQ.Messaging
{
    public class RabbitQueueProducerEndpointTests
    {
        [Fact]
        public void Equals_SameEndpointInstance_TrueIsReturned()
        {
            var endpoint = new RabbitQueueProducerEndpoint("endpoint")
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
            var endpoint1 = new RabbitQueueProducerEndpoint("endpoint")
            {
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false,
                    IsAutoDeleteEnabled = true,
                    IsExclusive = true
                }
            };

            var endpoint2 = new RabbitQueueProducerEndpoint("endpoint")
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
        public void Equals_DifferentName_FalseIsReturned()
        {
            var endpoint1 = new RabbitQueueProducerEndpoint("endpoint");
            var endpoint2 = new RabbitQueueProducerEndpoint("endpoint2");

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
        public void Equals_SameSerializerOptions_TrueIsReturned()
        {
            var endpoint1 = new RabbitQueueProducerEndpoint("endpoint")
            {
                Serializer = new JsonMessageSerializer
                {
                    Options =
                    {
                        MaxDepth = 100
                    }
                }
            };

            var endpoint2 = new RabbitQueueProducerEndpoint("endpoint")
            {
                Serializer = new JsonMessageSerializer
                {
                    Options =
                    {
                        MaxDepth = 100
                    }
                }
            };

            endpoint1.Equals(endpoint2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentSerializerOptions_FalseIsReturned()
        {
            var endpoint1 = new RabbitQueueProducerEndpoint("endpoint")
            {
                Serializer = new JsonMessageSerializer
                {
                    Options =
                    {
                        MaxDepth = 100
                    }
                }
            };

            var endpoint2 = new RabbitQueueProducerEndpoint("endpoint")
            {
                Serializer = new JsonMessageSerializer
                {
                    Options =
                    {
                        MaxDepth = 8
                    }
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }
    }
}
