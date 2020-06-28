// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging
{
    public class KafkaProducerEndpointTests
    {
        [Fact]
        public void Equals_SameEndpointInstance_TrueIsReturned()
        {
            var endpoint = new KafkaProducerEndpoint("endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    Acks = Acks.Leader
                }
            };

            endpoint.Equals(endpoint).Should().BeTrue();
        }

        [Fact]
        public void Equals_SameConfiguration_TrueIsReturned()
        {
            var endpoint1 = new KafkaProducerEndpoint("endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    Acks = Acks.Leader
                }
            };

            var endpoint2 = new KafkaProducerEndpoint("endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    Acks = Acks.Leader
                }
            };

            endpoint1.Equals(endpoint2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentName_FalseIsReturned()
        {
            var endpoint1 = new KafkaProducerEndpoint("endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    Acks = Acks.Leader
                }
            };

            var endpoint2 = new KafkaProducerEndpoint("endpoint2")
            {
                Configuration = new KafkaProducerConfig
                {
                    Acks = Acks.Leader
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentConfiguration_FalseIsReturned()
        {
            var endpoint1 = new KafkaProducerEndpoint("endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    Acks = Acks.Leader
                }
            };

            var endpoint2 = new KafkaProducerEndpoint("endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    Acks = Acks.All
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Equals_SameSerializerSettings_TrueIsReturned()
        {
            var endpoint1 = new KafkaProducerEndpoint("endpoint")
            {
                Serializer = new JsonMessageSerializer
                {
                    Settings =
                    {
                        MaxDepth = 100
                    }
                }
            };

            var endpoint2 = new KafkaProducerEndpoint("endpoint")
            {
                Serializer = new JsonMessageSerializer
                {
                    Settings =
                    {
                        MaxDepth = 100
                    }
                }
            };

            endpoint1.Equals(endpoint2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentSerializerSettings_FalseIsReturned()
        {
            var endpoint1 = new KafkaProducerEndpoint("endpoint")
            {
                Serializer = new JsonMessageSerializer
                {
                    Settings =
                    {
                        MaxDepth = 100
                    }
                }
            };

            var endpoint2 = new KafkaProducerEndpoint("endpoint")
            {
                Serializer = new JsonMessageSerializer
                {
                    Settings =
                    {
                        MaxDepth = 8
                    }
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }
    }
}
