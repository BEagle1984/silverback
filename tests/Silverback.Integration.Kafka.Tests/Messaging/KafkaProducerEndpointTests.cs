// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging
{
    public class KafkaProducerEndpointTests
    {
        [Theory]
        [InlineData(0, true)]
        [InlineData(42, true)]
        [InlineData(-1, true)]
        [InlineData(-2, false)]
        public void Constructor_Partition_CorrectlyValidated(int value, bool isValid)
        {
            KafkaProducerEndpoint? endpoint = null;

            Action act = () =>
            {
                endpoint = new KafkaProducerEndpoint("test", value)
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "test-server"
                    }
                };
            };

            if (isValid)
            {
                act.Should().NotThrow();
                endpoint.Should().NotBeNull();
            }
            else
            {
                act.Should().ThrowExactly<ArgumentOutOfRangeException>();
            }
        }

        [Fact]
        public void Equals_SameEndpointInstance_TrueIsReturned()
        {
            var endpoint = new KafkaProducerEndpoint("topic")
            {
                Configuration =
                {
                    Acks = Acks.Leader
                }
            };

            endpoint.Equals(endpoint).Should().BeTrue();
        }

        [Fact]
        public void Equals_SameConfiguration_TrueIsReturned()
        {
            var endpoint1 = new KafkaProducerEndpoint("topic")
            {
                Configuration =
                {
                    Acks = Acks.Leader
                }
            };

            var endpoint2 = new KafkaProducerEndpoint("topic")
            {
                Configuration =
                {
                    Acks = Acks.Leader
                }
            };

            endpoint1.Equals(endpoint2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentTopic_FalseIsReturned()
        {
            var endpoint1 = new KafkaProducerEndpoint("topic")
            {
                Configuration =
                {
                    Acks = Acks.Leader
                }
            };

            var endpoint2 = new KafkaProducerEndpoint("topic2")
            {
                Configuration =
                {
                    Acks = Acks.Leader
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Equals_SameTopicAndPartition_TrueIsReturned()
        {
            var endpoint1 = new KafkaProducerEndpoint("topic", 1);
            var endpoint2 = new KafkaProducerEndpoint("topic", 1);

            endpoint1.Equals(endpoint2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentConfiguration_FalseIsReturned()
        {
            var endpoint1 = new KafkaProducerEndpoint("topic")
            {
                Configuration =
                {
                    Acks = Acks.Leader
                }
            };

            var endpoint2 = new KafkaProducerEndpoint("topic")
            {
                Configuration =
                {
                    Acks = Acks.All
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Equals_SameSerializerSettings_TrueIsReturned()
        {
            var endpoint1 = new KafkaProducerEndpoint("topic")
            {
                Serializer = new JsonMessageSerializer
                {
                    Options =
                    {
                        MaxDepth = 100
                    }
                }
            };

            var endpoint2 = new KafkaProducerEndpoint("topic")
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
        public void Equals_DifferentSerializerSettings_FalseIsReturned()
        {
            var endpoint1 = new KafkaProducerEndpoint("topic")
            {
                Serializer = new JsonMessageSerializer
                {
                    Options =
                    {
                        MaxDepth = 100
                    }
                }
            };

            var endpoint2 = new KafkaProducerEndpoint("topic")
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

        [Fact]
        public void Validate_ValidTopicAndConfiguration_NoExceptionThrown()
        {
            var endpoint = GetValidEndpoint();

            Action act = () => endpoint.Validate();

            act.Should().NotThrow<EndpointConfigurationException>();
        }

        [Fact]
        public void Validate_InvalidConfiguration_ExceptionThrown()
        {
            var endpoint = new KafkaProducerEndpoint("topic");

            Action act = () => endpoint.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Validate_MissingTopic_ExceptionThrown()
        {
            var endpoint = new KafkaProducerEndpoint(string.Empty)
            {
                Configuration = new KafkaProducerConfig
                {
                    BootstrapServers = "test-server"
                }
            };

            Action act = () => endpoint.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        private static KafkaProducerEndpoint GetValidEndpoint() =>
            new("test")
            {
                Configuration = new KafkaProducerConfig
                {
                    BootstrapServers = "test-server"
                }
            };
    }
}
