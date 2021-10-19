// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Outbound.EndpointResolvers;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging;

public class MqttProducerConfigurationTests
{
    [Fact]
    public void Equals_SameEndpointInstance_TrueIsReturned()
    {
        MqttProducerConfiguration configuration = new()
        {
            Endpoint = new MqttStaticProducerEndpointResolver("topic"),
            Client = new MqttClientConfiguration
            {
                ClientId = "client1"
            }
        };

        configuration.Equals(configuration).Should().BeTrue();
    }

    [Fact]
    public void Equals_SameConfiguration_TrueIsReturned()
    {
        MqttProducerConfiguration configuration1 = new()
        {
            Endpoint = new MqttStaticProducerEndpointResolver("topic"),
            Client = new MqttClientConfiguration
            {
                ClientId = "client1"
            }
        };
        MqttProducerConfiguration configuration2 = new()
        {
            Endpoint = new MqttStaticProducerEndpointResolver("topic"),
            Client = new MqttClientConfiguration
            {
                ClientId = "client1"
            }
        };

        configuration1.Equals(configuration2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentTopic_FalseIsReturned()
    {
        MqttProducerConfiguration configuration1 = new()
        {
            Endpoint = new MqttStaticProducerEndpointResolver("topic1"),
            Client = new MqttClientConfiguration
            {
                ClientId = "client1"
            }
        };
        MqttProducerConfiguration configuration2 = new()
        {
            Endpoint = new MqttStaticProducerEndpointResolver("topic2"),
            Client = new MqttClientConfiguration
            {
                ClientId = "client1"
            }
        };

        configuration1.Equals(configuration2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentConfiguration_FalseIsReturned()
    {
        MqttProducerConfiguration configuration1 = new()
        {
            Endpoint = new MqttStaticProducerEndpointResolver("topic"),
            Client = new MqttClientConfiguration
            {
                ClientId = "client1"
            }
        };
        MqttProducerConfiguration configuration2 = new()
        {
            Endpoint = new MqttStaticProducerEndpointResolver("topic"),
            Client = new MqttClientConfiguration
            {
                ClientId = "client2"
            }
        };

        configuration1.Equals(configuration2).Should().BeFalse();
    }

    [Fact]
    public void Equals_SameSerializerSettings_TrueIsReturned()
    {
        MqttProducerConfiguration configuration1 = new()
        {
            Endpoint = new MqttStaticProducerEndpointResolver("topic"),
            Client = new MqttClientConfiguration
            {
                ClientId = "client2"
            },
            Serializer = new JsonMessageSerializer<object>
            {
                Options =
                {
                    MaxDepth = 100
                }
            }
        };
        MqttProducerConfiguration configuration2 = new()
        {
            Endpoint = new MqttStaticProducerEndpointResolver("topic"),
            Client = new MqttClientConfiguration
            {
                ClientId = "client2"
            },
            Serializer = new JsonMessageSerializer<object>
            {
                Options =
                {
                    MaxDepth = 100
                }
            }
        };

        configuration1.Equals(configuration2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentSerializerSettings_FalseIsReturned()
    {
        MqttProducerConfiguration configuration1 = new()
        {
            Endpoint = new MqttStaticProducerEndpointResolver("topic"),
            Client = new MqttClientConfiguration
            {
                ClientId = "client2"
            },
            Serializer = new JsonMessageSerializer<object>
            {
                Options =
                {
                    MaxDepth = 100
                }
            }
        };

        MqttProducerConfiguration configuration2 = new()
        {
            Endpoint = new MqttStaticProducerEndpointResolver("topic"),
            Client = new MqttClientConfiguration
            {
                ClientId = "client2"
            },
            Serializer = new JsonMessageSerializer<object>
            {
                Options =
                {
                    MaxDepth = 8
                }
            }
        };

        configuration1.Equals(configuration2).Should().BeFalse();
    }

    [Fact]
    public void Validate_ValidTopicAndConfiguration_NoExceptionThrown()
    {
        MqttProducerConfiguration configuration = GetValidConfiguration();

        Action act = () => configuration.Validate();

        act.Should().NotThrow<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_MissingConfiguration_ExceptionThrown()
    {
        MqttProducerConfiguration configuration = new()
        {
            Endpoint = new MqttStaticProducerEndpointResolver("topic"),
            Client = null!
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_InvalidConfiguration_ExceptionThrown()
    {
        MqttProducerConfiguration configuration = new()
        {
            Endpoint = new MqttStaticProducerEndpointResolver("topic"),
            Client = new MqttClientConfiguration()
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_MissingTopic_ExceptionThrown()
    {
        MqttProducerConfiguration configuration = new()
        {
            Client = new MqttClientConfiguration
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "test-server"
                }
            }
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_ChunkingEnabledOnV311_ExceptionThrown()
    {
        MqttProducerConfiguration configuration = new()
        {
            Endpoint = new MqttStaticProducerEndpointResolver("topic"),
            Client = new MqttClientConfiguration
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

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_ChunkingEnabledOnV500_ExceptionThrown()
    {
        MqttProducerConfiguration configuration = new()
        {
            Endpoint = new MqttStaticProducerEndpointResolver("topic"),
            Client = new MqttClientConfiguration
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

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    private static MqttProducerConfiguration GetValidConfiguration() =>
        new()
        {
            Endpoint = new MqttStaticProducerEndpointResolver("topic"),
            Client = new MqttClientConfiguration
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "test-server"
                }
            }
        };
}
