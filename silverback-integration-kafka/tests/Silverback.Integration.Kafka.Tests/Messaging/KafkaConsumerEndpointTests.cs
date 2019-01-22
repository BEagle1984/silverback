// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Newtonsoft.Json;
using Silverback.Messaging;
using Xunit;

namespace Silverback.Integration.Kafka.Tests.Messaging
{
    public class KafkaConsumerEndpointTests
    {
        [Fact]
        public void Equals_SameEndpointInstance_IsTrue()
        {
            var endpoint = new KafkaConsumerEndpoint("endpoint")
            {
                Configuration = new KafkaConsumerConfig
                {
                    AutoCommitIntervalMs = 1000
                }
            };

            endpoint.Equals(endpoint).Should().BeTrue();
        }

        [Fact]
        public void Equals_SameConfiguration_IsTrue()
        {
            var endpoint1 = new KafkaConsumerEndpoint("endpoint")
            {
                Configuration = new KafkaConsumerConfig
                {
                    AutoCommitIntervalMs = 1000
                }
            };

            var endpoint2 = new KafkaConsumerEndpoint("endpoint")
            {
                Configuration = new KafkaConsumerConfig
                {
                    AutoCommitIntervalMs = 1000
                }
            };

            endpoint1.Equals(endpoint2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentName_IsFalse()
        {
            var endpoint1 = new KafkaConsumerEndpoint("endpoint")
            {
                Configuration = new KafkaConsumerConfig
                {
                    AutoCommitIntervalMs = 1000
                }
            };

            var endpoint2 = new KafkaConsumerEndpoint("endpoint2")
            {
                Configuration = new KafkaConsumerConfig
                {
                    AutoCommitIntervalMs = 1000
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentConfiguration_IsFalse()
        {
            var endpoint1 = new KafkaConsumerEndpoint("endpoint")
            {
                Configuration = new KafkaConsumerConfig
                {
                    AutoCommitIntervalMs = 1000
                }
            };

            var endpoint2 = new KafkaConsumerEndpoint("endpoint")
            {
                Configuration = new KafkaConsumerConfig
                {
                    BrokerAddressTtl = 2000
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void IsSerializable()
        {
            var endpoint1 = new KafkaConsumerEndpoint("endpoint")
            {
                Configuration = new KafkaConsumerConfig
                {
                    AutoCommitIntervalMs = 1000
                }
            };

            var json = JsonConvert.SerializeObject(endpoint1,
                new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.Auto});

            var endpoint2 = JsonConvert.DeserializeObject<KafkaConsumerEndpoint>(json,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            endpoint2.Should().NotBeNull();
        }
    }
}
