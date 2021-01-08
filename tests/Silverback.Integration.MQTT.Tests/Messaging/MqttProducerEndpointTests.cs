// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging
{
    public class MqttProducerEndpointTests
    {
        [Fact]
        public void Equals_SameEndpointInstance_TrueIsReturned()
        {
            var endpoint = new MqttProducerEndpoint("topic")
            {
                Configuration =
                {
                    ClientId = "client1"
                }
            };

            endpoint.Equals(endpoint).Should().BeTrue();
        }

        [Fact]
        public void Equals_SameConfiguration_TrueIsReturned()
        {
            var endpoint1 = new MqttProducerEndpoint("topic")
            {
                Configuration =
                {
                    ClientId = "client1"
                }
            };

            var endpoint2 = new MqttProducerEndpoint("topic")
            {
                Configuration =
                {
                    ClientId = "client1"
                }
            };

            endpoint1.Equals(endpoint2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentTopic_FalseIsReturned()
        {
            var endpoint1 = new MqttProducerEndpoint("topic")
            {
                Configuration =
                {
                    ClientId = "client1"
                }
            };

            var endpoint2 = new MqttProducerEndpoint("topic2")
            {
                Configuration =
                {
                    ClientId = "client1"
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentConfiguration_FalseIsReturned()
        {
            var endpoint1 = new MqttProducerEndpoint("topic")
            {
                Configuration =
                {
                    ClientId = "client1"
                }
            };

            var endpoint2 = new MqttProducerEndpoint("topic")
            {
                Configuration =
                {
                    ClientId = "client2"
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Equals_SameSerializerSettings_TrueIsReturned()
        {
            var endpoint1 = new MqttProducerEndpoint("topic")
            {
                Configuration =
                {
                    ClientId = "client1"
                },
                Serializer = new JsonMessageSerializer
                {
                    Options =
                    {
                        MaxDepth = 100
                    }
                }
            };

            var endpoint2 = new MqttProducerEndpoint("topic")
            {
                Configuration =
                {
                    ClientId = "client1"
                },
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
            var endpoint1 = new MqttProducerEndpoint("topic")
            {
                Configuration =
                {
                    ClientId = "client1"
                },
                Serializer = new JsonMessageSerializer
                {
                    Options =
                    {
                        MaxDepth = 100
                    }
                }
            };

            var endpoint2 = new MqttProducerEndpoint("topic")
            {
                Configuration =
                {
                    ClientId = "client1"
                },
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
        public void Validate_MissingConfiguration_ExceptionThrown()
        {
            var endpoint = new MqttProducerEndpoint("topic")
            {
                Configuration = null!
            };

            Action act = () => endpoint.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Validate_InvalidConfiguration_ExceptionThrown()
        {
            var endpoint = new MqttProducerEndpoint("topic")
            {
                Configuration = new MqttClientConfig()
            };

            Action act = () => endpoint.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Validate_MissingTopic_ExceptionThrown()
        {
            var endpoint = new MqttProducerEndpoint(string.Empty)
            {
                Configuration = new MqttClientConfig
                {
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "test-server"
                    }
                }
            };

            Action act = () => endpoint.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Validate_ChunkingEnabledOnV311_ExceptionThrown()
        {
            var endpoint = new MqttProducerEndpoint("topic")
            {
                Configuration = new MqttClientConfig
                {
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "test-server"
                    },
                    ProtocolVersion = MqttProtocolVersion.V311
                },
                Chunk = new ChunkSettings
                {
                    Size = 10
                }
            };

            Action act = () => endpoint.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Validate_ChunkingEnabledOnV500_ExceptionThrown()
        {
            var endpoint = new MqttProducerEndpoint("topic")
            {
                Configuration = new MqttClientConfig
                {
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "test-server"
                    }
                },
                Chunk = new ChunkSettings
                {
                    Size = 10
                }
            };

            Action act = () => endpoint.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        private static MqttProducerEndpoint GetValidEndpoint() =>
            new("test")
            {
                Configuration = new MqttClientConfig
                {
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "test-server"
                    }
                }
            };
    }
}
