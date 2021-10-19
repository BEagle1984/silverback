// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using Silverback.Collections;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Outbound.EndpointResolvers;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging;

public class MqttConsumerConfigurationTests
{
    [Fact]
    public void RawName_SingleTopic_TopicNameReturned()
    {
        MqttConsumerConfiguration configuration = GetValidConfiguration() with
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic" })
        };

        configuration.RawName.Should().Be("topic");
    }

    [Fact]
    public void RawName_MultipleTopics_TopicNamesReturned()
    {
        MqttConsumerConfiguration configuration = GetValidConfiguration() with
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1", "topic2", "topic3" })
        };

        configuration.RawName.Should().Be("topic1,topic2,topic3");
    }

    [Fact]
    public void Equals_SameEndpointInstance_TrueIsReturned()
    {
        MqttConsumerConfiguration configuration = GetValidConfiguration();

        configuration.Equals(configuration).Should().BeTrue();
    }

    [Fact]
    public void Equals_SameConfiguration_TrueIsReturned()
    {
        MqttConsumerConfiguration configuration1 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1", "topic2", "topic3" }),
            Client = new MqttClientConfiguration
            {
                ClientId = "client1"
            }
        };
        MqttConsumerConfiguration configuration2 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1", "topic2", "topic3" }),
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
        MqttConsumerConfiguration configuration1 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1" }),
            Client = new MqttClientConfiguration
            {
                ClientId = "client1"
            }
        };
        MqttConsumerConfiguration configuration2 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic2" }),
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
        MqttConsumerConfiguration configuration1 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1" }),
            Client = new MqttClientConfiguration
            {
                ClientId = "client1"
            }
        };
        MqttConsumerConfiguration configuration2 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1" }),
            Client = new MqttClientConfiguration
            {
                ClientId = "client1",
                CleanSession = false
            }
        };

        configuration1.Equals(configuration2).Should().BeFalse();
    }

    [Fact]
    public void Validate_ValidTopicAndConfiguration_NoExceptionThrown()
    {
        MqttConsumerConfiguration configuration = GetValidConfiguration();

        Action act = () => configuration.Validate();

        act.Should().NotThrow<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_MissingConfiguration_ExceptionThrown()
    {
        MqttConsumerConfiguration configuration = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1" }),
            Client = null!
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_InvalidConfiguration_ExceptionThrown()
    {
        MqttConsumerConfiguration configuration = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1" }),
            Client = new MqttClientConfiguration()
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_MissingTopic_ExceptionThrown()
    {
        MqttConsumerConfiguration configuration = new()
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_InvalidTopicName_ExceptionThrown(string? topicName)
    {
        MqttConsumerConfiguration configuration = new()
        {
            Client = new MqttClientConfiguration
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "test-server"
                }
            },
            Topics = new ValueReadOnlyCollection<string>(new[] { topicName! })
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_RetryPolicyWithMultipleRetriesOnV311_NoExceptionThrown()
    {
        MqttConsumerConfiguration configuration = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1" }),
            Client = new MqttClientConfiguration
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "test-server"
                },
                ProtocolVersion = MqttProtocolVersion.V311
            },
            Serializer = new JsonMessageSerializer<TestEventOne>(),
            ErrorPolicy = new RetryErrorPolicy().MaxFailedAttempts(10)
        };

        Action act = () => configuration.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_MovePolicyWithMultipleRetriesOnV311_ExceptionThrown()
    {
        MqttConsumerConfiguration configuration = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1" }),
            Client = new MqttClientConfiguration
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "test-server"
                },
                ProtocolVersion = MqttProtocolVersion.V311
            },
            Serializer = new JsonMessageSerializer<TestEventOne>(),
            ErrorPolicy = new MoveMessageErrorPolicy(GetValidProducerConfiguration()).MaxFailedAttempts(10)
        };

        Action act = () => configuration.Validate();

        act.Should().Throw<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_ChainedRetryPolicyWithMultipleRetriesOnV311_NoExceptionThrown()
    {
        MqttConsumerConfiguration configuration = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1" }),
            Client = new MqttClientConfiguration
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "test-server"
                },
                ProtocolVersion = MqttProtocolVersion.V311
            },
            Serializer = new JsonMessageSerializer<TestEventOne>(),
            ErrorPolicy = new ErrorPolicyChain(new RetryErrorPolicy().MaxFailedAttempts(10))
        };

        Action act = () => configuration.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ChainedMovePolicyWithMultipleRetriesOnV311_ExceptionThrown()
    {
        MqttConsumerConfiguration configuration = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1" }),
            Client = new MqttClientConfiguration
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "test-server"
                },
                ProtocolVersion = MqttProtocolVersion.V311
            },
            Serializer = new JsonMessageSerializer<TestEventOne>(),
            ErrorPolicy = new ErrorPolicyChain(new MoveMessageErrorPolicy(GetValidProducerConfiguration()).MaxFailedAttempts(10))
        };

        Action act = () => configuration.Validate();

        act.Should().Throw<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_DynamicTypeSerializerOnV311_ExceptionThrown()
    {
        MqttConsumerConfiguration configuration = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1" }),
            Client = new MqttClientConfiguration
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "test-server"
                },
                ProtocolVersion = MqttProtocolVersion.V311
            },
            Serializer = new JsonMessageSerializer<object>()
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_FixedTypeSerializerOnV311_NoExceptionThrown()
    {
        MqttConsumerConfiguration configuration = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1" }),
            Client = new MqttClientConfiguration
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "test-server"
                },
                ProtocolVersion = MqttProtocolVersion.V311
            },
            Serializer = new JsonMessageSerializer<TestEventOne>()
        };

        Action act = () => configuration.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_RetryPolicyWithMultipleRetriesOnV500_NoExceptionThrown()
    {
        MqttConsumerConfiguration configuration = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1" }),
            Client = new MqttClientConfiguration
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "test-server"
                },
                ProtocolVersion = MqttProtocolVersion.V500
            },
            ErrorPolicy = new RetryErrorPolicy().MaxFailedAttempts(10)
        };

        Action act = () => configuration.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_MovePolicyWithMultipleRetriesOnV500_NoExceptionThrown()
    {
        MqttConsumerConfiguration configuration = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1" }),
            Client = new MqttClientConfiguration
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "test-server"
                },
                ProtocolVersion = MqttProtocolVersion.V500
            },
            ErrorPolicy =
                new MoveMessageErrorPolicy(GetValidProducerConfiguration()).MaxFailedAttempts(10)
        };

        Action act = () => configuration.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ChainedRetryPolicyWithMultipleRetriesOnV500_NoExceptionThrown()
    {
        MqttConsumerConfiguration configuration = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1" }),
            Client = new MqttClientConfiguration
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "test-server"
                },
                ProtocolVersion = MqttProtocolVersion.V500
            },
            ErrorPolicy = new ErrorPolicyChain(new RetryErrorPolicy().MaxFailedAttempts(10))
        };

        Action act = () => configuration.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ChainedMovePolicyWithMultipleRetriesOnV500_NoExceptionThrown()
    {
        MqttConsumerConfiguration configuration = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1" }),
            Client = new MqttClientConfiguration
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "test-server"
                },
                ProtocolVersion = MqttProtocolVersion.V500
            },
            ErrorPolicy = new ErrorPolicyChain(new MoveMessageErrorPolicy(GetValidProducerConfiguration()).MaxFailedAttempts(10))
        };

        Action act = () => configuration.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_DynamicTypeSerializerOnV500_NoExceptionThrown()
    {
        MqttConsumerConfiguration configuration = new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "topic1" }),
            Client = new MqttClientConfiguration
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "test-server"
                },
                ProtocolVersion = MqttProtocolVersion.V500
            },
            Serializer = new JsonMessageSerializer<object>()
        };

        Action act = () => configuration.Validate();

        act.Should().NotThrow();
    }

    private static MqttConsumerConfiguration GetValidConfiguration() =>
        new()
        {
            Topics = new ValueReadOnlyCollection<string>(new[] { "test" }),
            Client = new MqttClientConfiguration
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "test-server"
                }
            }
        };

    private static MqttProducerConfiguration GetValidProducerConfiguration() =>
        new()
        {
            Endpoint = new MqttStaticProducerEndpointResolver("test"),
            Client = new MqttClientConfiguration
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "test-server"
                }
            }
        };
}
