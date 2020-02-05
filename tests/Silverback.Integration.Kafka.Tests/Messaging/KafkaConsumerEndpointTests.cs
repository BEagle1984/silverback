// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Newtonsoft.Json;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging
{
    public class KafkaConsumerEndpointTests
    {
        [Fact]
        public void Equals_SameEndpointInstance_TrueIsReturned()
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
        public void Equals_SameConfiguration_TrueIsReturned()
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
        public void Equals_DeserializedEndpoint_TrueIsReturned()
        {
            var endpoint1 = new KafkaConsumerEndpoint("endpoint")
            {
                Configuration = new KafkaConsumerConfig
                {
                    AutoCommitIntervalMs = 1000
                }
            };

            var json = JsonConvert.SerializeObject(endpoint1,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            var endpoint2 = JsonConvert.DeserializeObject<KafkaConsumerEndpoint>(json,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            endpoint1.Equals(endpoint2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentName_FalseIsReturned()
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
        public void Equals_DifferentConfiguration_FalseIsReturned()
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
        public void SerializationAndDeserialization_NoInformationIsLost()
        {
            var endpoint1 = new KafkaConsumerEndpoint("endpoint")
            {
                Configuration = new KafkaConsumerConfig
                {
                    CommitOffsetEach = 5,
                    AutoCommitIntervalMs = 1000
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

            var endpoint2 = JsonConvert.DeserializeObject<KafkaConsumerEndpoint>(json,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            endpoint2.Should().BeEquivalentTo(endpoint1);
        }
    }
}