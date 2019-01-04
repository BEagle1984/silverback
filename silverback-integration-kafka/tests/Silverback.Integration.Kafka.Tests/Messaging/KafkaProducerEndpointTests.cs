// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using NUnit.Framework;
using Silverback.Messaging;

namespace Silverback.Integration.Kafka.Tests.Messaging
{
    [TestFixture]
    public class KafkaProducerEndpointTests
    {
        [Test]
        public void Equals_SameEndpointInstance_IsTrue()
        {
            var endpoint = new KafkaProducerEndpoint("endpoint")
            {
                Configuration = new KafkaProducerConfig
                {
                     Acks = 3
                }
            };

            var equals = endpoint.Equals(endpoint);
            Assert.That(equals, Is.True);
        }

        [Test]
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

            var equals = endpoint1.Equals(endpoint2);
            Assert.That(equals, Is.True);
        }

        [Test]
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

            var equals = endpoint1.Equals(endpoint2);
            Assert.That(equals, Is.False);
        }

        [Test]
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

            var equals = endpoint1.Equals(endpoint2);
            Assert.That(equals, Is.False);
        }
    }
}