// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Newtonsoft.Json;
using Silverback.Messaging;
using Xunit;

namespace Silverback.Integration.Kafka.Tests.Messaging
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
                     Acks = 3
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
                    Acks = 3
                }
            };

            var endpoint2 = new KafkaProducerEndpoint("endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    Acks = 3
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
                    Acks = 3
                }
            };

            var endpoint2 = new KafkaProducerEndpoint("endpoint2")
            {
                Configuration = new KafkaProducerConfig
                {
                    Acks = 3
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
                    Acks = 3
                }
            };

            var endpoint2 = new KafkaProducerEndpoint("endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                    Acks = 2
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
                    Acks = 2
                }
            };

            var json = JsonConvert.SerializeObject(endpoint1,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            var endpoint2 = JsonConvert.DeserializeObject<KafkaProducerEndpoint>(json,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            endpoint2.Should().NotBeNull();
        }
    }
}