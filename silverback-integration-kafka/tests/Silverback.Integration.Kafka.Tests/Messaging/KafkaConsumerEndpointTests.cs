// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using NUnit.Framework;
using Silverback.Messaging;

namespace Silverback.Integration.Kafka.Tests.Messaging
{
    [TestFixture]
    public class KafkaConsumerEndpointTests
    {
        [Test]
        public void Equals_SameEndpointInstance_IsTrue()
        {
            var endpoint = new KafkaConsumerEndpoint("endpoint")
            {
                Configuration = new KafkaConsumerConfig
                {
                    AutoCommitIntervalMs = 1000
                }
            };

            var equals = endpoint.Equals(endpoint);
            Assert.That(equals, Is.True);
        }

        [Test]
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

            var equals = endpoint1.Equals(endpoint2);
            Assert.That(equals, Is.True);
        }

        [Test]
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

            var equals = endpoint1.Equals(endpoint2);
            Assert.That(equals, Is.False);
        }

        [Test]
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

            var equals = endpoint1.Equals(endpoint2);
            Assert.That(equals, Is.False);
        }
    }
}
