// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using Shouldly;
using Silverback.Collections;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttClientConfigurationTests
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
    [InlineData("topic1", "$share/group/topic1")]
    [InlineData("$share/group1/topic1", "$share/group2/topic1")]
    [InlineData("topic1/#", "$share/group/topic1/#")]
    public void Validate_ShouldThrow_WhenTopicIsSubscribedMoreThanOnceThroughSharedSubscriptionInSameEndpoint(string topic1, string topic2)
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>(
            [
                new MqttConsumerEndpointConfiguration
                {
                    Topics = new ValueReadOnlyCollection<string>([topic1, topic2])
                }
            ])
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldMatch(".*subscribe it more than once through shared subscriptions.*");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSameTopicIsRepeatedInSameEndpoint()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>(
            [
                new MqttConsumerEndpointConfiguration
                {
                    Topics = new ValueReadOnlyCollection<string>(["$share/group/topic1", "$share/group/topic1"])
                }
            ])
        };

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData("topic1", "$share/group/topic1")]
    [InlineData("$share/group1/topic1", "$share/group2/topic1")]
    [InlineData("topic1/#", "$share/group/topic1/#")]
    public void Validate_ShouldThrow_WhenTopicIsSpecifiedMoreThanOnceThroughSharedSubscription(string topic1, string topic2)
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>(
            [
                new MqttConsumerEndpointConfiguration
                {
                    Topics = new ValueReadOnlyCollection<string>([topic1])
                },
                new MqttConsumerEndpointConfiguration
                {
                    Topics = new ValueReadOnlyCollection<string>([topic2])
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
        exception.Message.ShouldMatch("ClientId is required to connect with the message broker.*");
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
        exception.Message.ShouldMatch("Channel configuration is required.*");
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
        exception.Message.ShouldMatch("Wrong deserializer configuration. Since headers.*");
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
        exception.Message.ShouldBe("MaxDegreeOfParallelism must be greater or equal to 1.");
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
        exception.Message.ShouldBe("BackpressureLimit must be greater or equal to 1.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldThrow_WhenTimeoutIsLessThanOrEqualToZero(int value)
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            Timeout = TimeSpan.FromSeconds(value)
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("Timeout must be greater than zero.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldThrow_WhenAcknowledgmentTimeoutIsLessThanOrEqualToZero(int value)
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            AcknowledgmentTimeout = TimeSpan.FromSeconds(value)
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("AcknowledgmentTimeout must be greater than zero.");
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtMostOnce, null)]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, 1)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, 1)]
    public void Validate_ShouldNotThrow_WhenBatchProcessingIsValid(MqttQualityOfServiceLevel qos, int? maxWaitTimeSeconds)
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            MaxDegreeOfParallelism = 10,
            ConsumerEndpoints = GetBatchConsumerEndpoints(
                qos,
                maxWaitTimeSeconds == null ? null : TimeSpan.FromSeconds(maxWaitTimeSeconds.Value),
                "topic1",
                "topic2")
        };

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenBatchProcessingIsEnabledWithProtocolPriorTo500()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ProtocolVersion = MqttProtocolVersion.V311,
            ConsumerEndpoints = GetBatchConsumerEndpoints(MqttQualityOfServiceLevel.AtMostOnce, null, "topic1")
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("Batch processing requires MQTT 5.");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenBatchSizeIsOneWithProtocolPriorTo500()
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
                    Batch = new BatchSettings { Size = 1 }
                }
            ])
        };

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointsHaveDifferentBatchSettings()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>(
            [
                .. GetBatchConsumerEndpoints(MqttQualityOfServiceLevel.AtMostOnce, null, "topic1"),
                new MqttConsumerEndpointConfiguration
                {
                    Topics = new ValueReadOnlyCollection<string>(["topic2"])
                }
            ])
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe(
            "All consumer endpoints of the client must use the same batch settings. " +
            "Use separate clients for the endpoints with different batch settings.");
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce)]
    public void Validate_ShouldThrow_WhenBatchMaxWaitTimeIsNotSetWithQosGreaterThanZero(MqttQualityOfServiceLevel qos)
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ConsumerEndpoints = GetBatchConsumerEndpoints(qos, null, "topic1")
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe(
            "The batch MaxWaitTime must be set when consuming with QoS 1 or 2, since the broker stops delivering when too many " +
            "messages are waiting for acknowledgment, which could prevent the batch from being completed.");
    }

    [Theory]
    [InlineData(0, 6554, 10, 65535)]
    [InlineData(100, 11, 10, 100)]
    public void Validate_ShouldThrow_WhenBatchedMessagesExceedReceiveMaximum(
        int receiveMaximum,
        int maxDegreeOfParallelism,
        int batchSize,
        int effectiveReceiveMaximum)
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ReceiveMaximum = (ushort)receiveMaximum,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            ConsumerEndpoints = GetBatchConsumerEndpoints(MqttQualityOfServiceLevel.AtLeastOnce, TimeSpan.FromSeconds(1), "topic1")
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe(
            $"The batch size ({batchSize}) multiplied by the MaxDegreeOfParallelism ({maxDegreeOfParallelism}) exceeds the " +
            $"ReceiveMaximum ({effectiveReceiveMaximum}), which limits the number of messages that can be waiting for acknowledgment. " +
            "Lower the batch size or the MaxDegreeOfParallelism, or raise the ReceiveMaximum (see LimitUnacknowledgedPublications).");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenBatchedMessagesMatchReceiveMaximum()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ReceiveMaximum = 100,
            MaxDegreeOfParallelism = 10,
            ConsumerEndpoints = GetBatchConsumerEndpoints(MqttQualityOfServiceLevel.AtLeastOnce, TimeSpan.FromSeconds(1), "topic1")
        };

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenBatchedMessagesExceedReceiveMaximumWithQos0()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ReceiveMaximum = 10,
            MaxDegreeOfParallelism = 10,
            ConsumerEndpoints = GetBatchConsumerEndpoints(MqttQualityOfServiceLevel.AtMostOnce, null, "topic1")
        };

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenChunkingIsEnabled()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ProducerEndpoints = GetChunkingProducerEndpoints()
        };

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointsHaveDifferentBatchMaxWaitTime()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>(
            [
                .. GetBatchConsumerEndpoints(MqttQualityOfServiceLevel.AtLeastOnce, TimeSpan.FromSeconds(1), "topic1"),
                .. GetBatchConsumerEndpoints(MqttQualityOfServiceLevel.AtLeastOnce, TimeSpan.FromSeconds(2), "topic2")
            ])
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldStartWith("All consumer endpoints of the client must use the same batch settings.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenBatchMaxWaitTimeIsNotSetAndAnyEndpointHasQosGreaterThanZero()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>(
            [
                .. GetBatchConsumerEndpoints(MqttQualityOfServiceLevel.AtMostOnce, null, "topic1"),
                .. GetBatchConsumerEndpoints(MqttQualityOfServiceLevel.AtLeastOnce, null, "topic2")
            ])
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldStartWith("The batch MaxWaitTime must be set when consuming with QoS 1 or 2");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenChunkingIsEnabledForRetainedMessages()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ProducerEndpoints = new ValueReadOnlyCollection<MqttProducerEndpointConfiguration>(
            [
                GetChunkingProducerEndpoints().Single() with { Retain = true }
            ])
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("Chunking can't be enabled for retained messages, since only the last chunk is retained.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenChunkingIsEnabledWithProtocolPriorTo500()
    {
        MqttClientConfiguration configuration = GetValidConfiguration() with
        {
            ProtocolVersion = MqttProtocolVersion.V311,
            ProducerEndpoints = GetChunkingProducerEndpoints()
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("Chunking requires MQTT 5.");
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
#pragma warning disable CS0618 // Type or member is obsolete
            new MQTTnet.Packets.MqttUserProperty("key1", "value1"),
            new MQTTnet.Packets.MqttUserProperty("key2", "value2")
#pragma warning restore CS0618 // Type or member is obsolete
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

    private static ValueReadOnlyCollection<MqttConsumerEndpointConfiguration> GetBatchConsumerEndpoints(
        MqttQualityOfServiceLevel qos,
        TimeSpan? maxWaitTime,
        params string[] topics) =>
        new(
        [
            .. topics.Select(topic => new MqttConsumerEndpointConfiguration
            {
                Topics = new ValueReadOnlyCollection<string>([topic]),
                Deserializer = new JsonMessageDeserializer<TestEventOne>(),
                QualityOfServiceLevel = qos,
                Batch = new BatchSettings { Size = 10, MaxWaitTime = maxWaitTime }
            })
        ]);

    private static ValueReadOnlyCollection<MqttProducerEndpointConfiguration> GetChunkingProducerEndpoints() =>
        new(
        [
            new MqttProducerEndpointConfiguration
            {
                EndpointResolver = new MqttStaticProducerEndpointResolver("topic1"),
                Serializer = new JsonMessageSerializer(),
                Chunk = new ChunkSettings { Size = 42 }
            }
        ]);
}
