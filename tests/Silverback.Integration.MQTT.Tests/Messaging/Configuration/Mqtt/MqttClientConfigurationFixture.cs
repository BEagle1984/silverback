// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Net;
using MQTTnet;
using MQTTnet.Formatter;
using Shouldly;
using Silverback.Collections;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttClientConfigurationFixture
{
    [Fact]
    public void Constructor_ShouldSetProtocolVersionToV500()
    {
        MqttClientConfiguration configuration = new();

        configuration.ProtocolVersion.ShouldBe(MqttProtocolVersion.V500);
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        MqttClientConfiguration configuration = GetValidConfiguration();

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenProducerEndpointsIsNull()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ProducerEndpoints = null!
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("ProducerEndpoints cannot be null.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenConsumerEndpointsIsNull()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ConsumerEndpoints = null!
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("ConsumerEndpoints cannot be null.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenProducerEndpointsAndConsumerEndpointsAreEmpty()
    {
        MqttClientConfiguration configuration = new();

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("At least one endpoint must be configured.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenProducerEndpointIsNotValid()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ProducerEndpoints = new ValueReadOnlyCollection<MqttProducerEndpointConfiguration>(
            [
                new MqttProducerEndpointConfiguration
                {
                    EndpointResolver = null!
                }
            ])
        };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenConsumerEndpointIsNotValid()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>(
            [
                new MqttConsumerEndpointConfiguration
                {
                    Topics = null!
                }
            ])
        };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenTopicIsSpecifiedMoreThanOnce()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>(
            [
                new MqttConsumerEndpointConfiguration
                {
                    Topics = new ValueReadOnlyCollection<string>(["topic1", "topic2"])
                },
                new MqttConsumerEndpointConfiguration
                {
                    Topics = new ValueReadOnlyCollection<string>(["topic2"])
                }
            ])
        };
        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldMatch("Cannot connect to the same topic in different endpoints.*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_ShouldThrow_WhenClientIdIsNullOrEmpty(string? clientId)
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ClientId = clientId!
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldMatch("A ClientId is required to connect with the message broker.*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenChannelIsNull()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            Channel = null
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldMatch("The channel configuration is required.*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenChannelIsNotValid()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            Channel = new MqttClientTcpConfiguration { RemoteEndpoint = null! }
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldMatch("The remote endpoint is required.*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenUserPropertyIsNotValid()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            UserProperties = new ValueReadOnlyCollection<MqttUserProperty>(
            [
                new MqttUserProperty(null!, null!)
            ])
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("The name of a user property cannot be empty.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenWillMessageIsNotValid()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            WillMessage = new MqttLastWillMessageConfiguration
            {
                Payload = new byte[10],
                Topic = null!
            }
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldMatch("The topic is required.*");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenProtocolIs300AndProducerSerializerRequiresHeaders()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ProtocolVersion = MqttProtocolVersion.V311,
            ProducerEndpoints = new ValueReadOnlyCollection<MqttProducerEndpointConfiguration>(
            [
                new MqttProducerEndpointConfiguration
                {
                    EndpointResolver = new MqttStaticProducerEndpointResolver("topic1"),
                    Serializer = new JsonMessageSerializer()
                }
            ])
        };

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenProtocolIs300AndConsumerDeserializerRequiresHeaders()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ProtocolVersion = MqttProtocolVersion.V311,
            ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>(
            [
                new MqttConsumerEndpointConfiguration
                {
                    Topics = new ValueReadOnlyCollection<string>(["topic1"]),
                    Deserializer = new JsonMessageDeserializer<object>()
                }
            ])
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldMatch("Wrong serializer configuration. Since headers.*");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenProtocolIs300ButDeserializersDoNotRequireHeaders()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ProtocolVersion = MqttProtocolVersion.V311,
            ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>(
            [
                new MqttConsumerEndpointConfiguration
                {
                    Topics = new ValueReadOnlyCollection<string>(["topic1"]),
                    Deserializer = new JsonMessageDeserializer<TestEventOne>()
                }
            ])
        };

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenProtocolIs300AndErrorPolicyRequiresHeaders()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ProtocolVersion = MqttProtocolVersion.V311,
            ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>(
            [
                new MqttConsumerEndpointConfiguration
                {
                    Topics = new ValueReadOnlyCollection<string>(["topic1"]),
                    Deserializer = new JsonMessageDeserializer<TestEventOne>(),
                    ErrorPolicy = new ErrorPolicyChain(
                        new MoveMessageErrorPolicy("topic2")
                        {
                            MaxFailedAttempts = 3
                        })
                }
            ])
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldMatch("Cannot set MaxFailedAttempts on the error policies.*");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenProtocolIs300AndErrorPolicyIsRetry()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ProtocolVersion = MqttProtocolVersion.V311,
            ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>(
            [
                new MqttConsumerEndpointConfiguration
                {
                    Topics = new ValueReadOnlyCollection<string>(["topic1"]),
                    Deserializer = new JsonMessageDeserializer<TestEventOne>(),
                    ErrorPolicy = new ErrorPolicyChain(new RetryErrorPolicy { MaxFailedAttempts = 5 })
                }
            ])
        };

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldThrow_WhenMaxDegreeOfParallelismIsLessThanOne(int value)
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            MaxDegreeOfParallelism = value
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("The maximum degree of parallelism must be greater or equal to 1.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldThrow_WhenBackpressureLimitIsLessThanOne(int value)
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            BackpressureLimit = value
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("The backpressure limit must be greater or equal to 1.");
    }

    [Fact]
    public void GetMqttClientOptions_ShouldReturnClientOptions()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ClientId = "client42",
            ProtocolVersion = MqttProtocolVersion.V310,
            Channel = new MqttClientWebSocketConfiguration { Uri = "test-server" },
            WillMessage = new MqttLastWillMessageConfiguration
            {
                Topic = "topic1"
            }
        };

        MqttClientOptions options = configuration.GetMqttClientOptions();

        options.ClientId.ShouldBe("client42");
        options.ProtocolVersion.ShouldBe(MqttProtocolVersion.V310);
        MqttClientWebSocketOptions webSocketOptions = options.ChannelOptions.ShouldBeOfType<MqttClientWebSocketOptions>();
        webSocketOptions.Uri.ShouldBe("test-server");
        options.WillTopic.ShouldBe("topic1");
    }

    [Fact]
    public void GetMqttClientOptions_ShouldReturnUserProperties()
    {
        List<MqttUserProperty> mqttUserProperties =
        [
            new("key1", "value1"),
            new("key2", "value2")
        ];

        MqttClientConfiguration configuration = new()
        {
            UserProperties = mqttUserProperties.AsValueReadOnlyCollection()
        };

        MqttClientOptions options = configuration.GetMqttClientOptions();

        options.UserProperties.ShouldBe(
        [
            new MQTTnet.Packets.MqttUserProperty("key1", "value1"),
            new MQTTnet.Packets.MqttUserProperty("key2", "value2")
        ]);
    }

    private static MqttClientConfiguration GetValidConfiguration() =>
        new()
        {
            Channel = new MqttClientTcpConfiguration
            {
                RemoteEndpoint = new DnsEndPoint("test", 1883)
            },
            ClientId = "client42",
            ProducerEndpoints = new ValueReadOnlyCollection<MqttProducerEndpointConfiguration>(
            [
                new MqttProducerEndpointConfiguration
                {
                    EndpointResolver = new MqttStaticProducerEndpointResolver("topic1"),
                    Serializer = new JsonMessageSerializer()
                }
            ])
        };
}
