// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Newtonsoft.Json;
using Silverback.Messaging;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging
{
    public class KafkaProducerEndpointTests
    {
        [Fact]
        public void Equals_SameEndpointInstance_IsTrue()
        {
            var endpoint = new KafkaProducerEndpoint("endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                     Acks = Confluent.Kafka.Acks.Leader
                }
            };

            endpoint.Equals(endpoint).Should().BeTrue();
        }

        [Fact]
        public void Equals_SameConfiguration_IsTrue()
        {
            var endpoint1 = new KafkaProducerEndpoint("endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    Acks = Confluent.Kafka.Acks.Leader
                }
            };

            var endpoint2 = new KafkaProducerEndpoint("endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    Acks = Confluent.Kafka.Acks.Leader
                }
            };

            endpoint1.Equals(endpoint2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentName_IsFalse()
        {
            var endpoint1 = new KafkaProducerEndpoint("endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    Acks = Confluent.Kafka.Acks.Leader
                }
            };

            var endpoint2 = new KafkaProducerEndpoint("endpoint2")
            {
                Configuration = new KafkaProducerConfig
                {
                    Acks = Confluent.Kafka.Acks.Leader
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentConfiguration_IsFalse()
        {
            var endpoint1 = new KafkaProducerEndpoint("endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    Acks = Confluent.Kafka.Acks.Leader
                }
            };

            var endpoint2 = new KafkaProducerEndpoint("endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    Acks = Confluent.Kafka.Acks.All
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void IsSerializable()
        {
            var endpoint1 = new KafkaProducerEndpoint("endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    Acks = Confluent.Kafka.Acks.All
                }
            };

            var json = JsonConvert.SerializeObject(endpoint1,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            var endpoint2 = JsonConvert.DeserializeObject<KafkaProducerEndpoint>(json,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            endpoint2.Should().NotBeNull();
            endpoint2.Configuration.Acks.Should().Be(endpoint1.Configuration.Acks);
        }
    }
}