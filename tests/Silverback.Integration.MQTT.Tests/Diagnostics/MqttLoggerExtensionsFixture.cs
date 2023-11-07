// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using NSubstitute;
using Silverback.Collections;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Messaging.Producing.Routing;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Logging;
using Xunit;
using MqttUserProperty = MQTTnet.Packets.MqttUserProperty;

namespace Silverback.Tests.Integration.Mqtt.Diagnostics;

public sealed class MqttLoggerExtensionsFixture : IDisposable
{
    private readonly LoggerSubstitute<MqttLoggerExtensionsFixture> _loggerSubstitute;

    private readonly ISilverbackLogger<MqttLoggerExtensionsFixture> _silverbackLogger;

    private readonly MqttClientWrapper _mqttClientWrapper;

    private readonly MqttConsumer _consumer;

    private readonly MqttProducer _producer;

    public MqttLoggerExtensionsFixture()
    {
        _loggerSubstitute = new LoggerSubstitute<MqttLoggerExtensionsFixture>(LogLevel.Trace);
        MappedLevelsLogger<MqttLoggerExtensionsFixture> mappedLevelsLogger = new(new LogLevelDictionary(), _loggerSubstitute);
        _silverbackLogger = new SilverbackLogger<MqttLoggerExtensionsFixture>(mappedLevelsLogger);

        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ISequenceStore)).Returns(Substitute.For<ISequenceStore>());

        _mqttClientWrapper = new MqttClientWrapper(
            "client1",
            Substitute.For<IMqttClient>(),
            new MqttClientConfiguration
            {
                ClientId = "id1",
                Channel = new MqttClientTcpConfiguration
                {
                    Server = "test-broker",
                    Port = 1234
                }
            },
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            _silverbackLogger);

        _consumer = new MqttConsumer(
            "consumer1",
            _mqttClientWrapper,
            new MqttClientConfiguration(),
            Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
            serviceProvider,
            Substitute.For<IConsumerLogger<MqttConsumer>>());

        _producer = new MqttProducer(
            "producer1",
            _mqttClientWrapper,
            new MqttClientConfiguration
            {
                ProducerEndpoints = new ValueReadOnlyCollection<MqttProducerEndpointConfiguration>(
                    new[]
                    {
                        new MqttProducerEndpointConfiguration
                        {
                            Endpoint = new MqttStaticProducerEndpointResolver("topic1")
                        }
                    })
            },
            Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>(),
            Substitute.For<IOutboundEnvelopeFactory>(),
            serviceProvider,
            Substitute.For<IProducerLogger<MqttProducer>>());
    }

    [Fact]
    public void LogConsuming_ShouldLog()
    {
        ConsumedApplicationMessage applicationMessage = new(
            new MqttApplicationMessageReceivedEventArgs(
                "client1",
                new MqttApplicationMessage
                {
                    Topic = "some-topic",
                    UserProperties = new List<MqttUserProperty>
                    {
                        new(DefaultMessageHeaders.MessageId, "123")
                    }
                },
                new MqttPublishPacket(),
                (_, _) => Task.CompletedTask));

        _silverbackLogger.LogConsuming(applicationMessage, _consumer);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            $"Consuming message {applicationMessage.Id} from topic some-topic. | consumerName: consumer1",
            4011);
    }

    [Fact]
    public void LogAcknowledgeFailed_ShouldLog()
    {
        ConsumedApplicationMessage applicationMessage = new(
            new MqttApplicationMessageReceivedEventArgs(
                "client1",
                new MqttApplicationMessage
                {
                    Topic = "some-topic",
                    UserProperties = new List<MqttUserProperty>
                    {
                        new(DefaultMessageHeaders.MessageId, "123")
                    }
                },
                new MqttPublishPacket(),
                (_, _) => Task.CompletedTask));

        _silverbackLogger.LogAcknowledgeFailed(applicationMessage, _consumer, new TimeoutException());

        _loggerSubstitute.Received(
            LogLevel.Warning,
            typeof(TimeoutException),
            $"Failed to acknowledge message {applicationMessage.Id} from topic some-topic. | consumerName: consumer1",
            4012);
    }

    [Fact]
    public void LogConnectError_ShouldLog()
    {
        _silverbackLogger.LogConnectError(_mqttClientWrapper, new MqttCommunicationException("test"));

        _loggerSubstitute.Received(
            LogLevel.Warning,
            typeof(MqttCommunicationException),
            "Error occurred connecting to the MQTT broker. | clientName: client1, clientId: id1, broker: test-broker:1234",
            4021);
    }

    [Fact]
    public void LogConnectRetryError_ShouldLog()
    {
        _silverbackLogger.LogConnectRetryError(_mqttClientWrapper, new MqttCommunicationException("test"));

        _loggerSubstitute.Received(
            LogLevel.Debug,
            typeof(MqttCommunicationException),
            "Error occurred retrying to connect to the MQTT broker. | clientName: client1, clientId: id1, broker: test-broker:1234",
            4022);
    }

    [Fact]
    public void LogConnectionLost_ShouldLog()
    {
        _silverbackLogger.LogConnectionLost(_mqttClientWrapper);

        _loggerSubstitute.Received(
            LogLevel.Warning,
            null,
            "Connection with the MQTT broker lost. The client will try to reconnect. | clientName: client1, clientId: id1, broker: test-broker:1234",
            4023);
    }

    [Fact]
    public void LogReconnected_ShouldLog()
    {
        _silverbackLogger.LogReconnected(_mqttClientWrapper);

        _loggerSubstitute.Received(
            LogLevel.Information,
            null,
            "Connection with the MQTT broker reestablished. | clientName: client1, clientId: id1, broker: test-broker:1234",
            4024);
    }

    [Fact]
    public void LogProducerQueueProcessingCanceled_ShouldLog()
    {
        _silverbackLogger.LogProducerQueueProcessingCanceled(_mqttClientWrapper);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Producer queue processing was canceled. | clientName: client1, clientId: id1",
            4031);
    }

    [Fact]
    public void LogConsumerSubscribed_ShouldLog()
    {
        _silverbackLogger.LogConsumerSubscribed("my/topic/#", _consumer);

        _loggerSubstitute.Received(
            LogLevel.Information,
            null,
            "Consumer subscribed to my/topic/#. | clientName: client1, clientId: id1, consumerName: consumer1",
            4041);
    }

    [Fact]
    public void LogMqttClientError_ShouldLog()
    {
        _silverbackLogger.LogMqttClientError(
            "source",
            "error {0} occurred",
            new object[] { 42 },
            new InvalidOperationException("test"));

        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(InvalidOperationException),
            "Error from MqttClient (source): 'error 42 occurred'.",
            4101);
    }

    [Fact]
    public void LogMqttClientWarning_ShouldLog()
    {
        _silverbackLogger.LogMqttClientWarning(
            "source",
            "warning {0}",
            new object[] { 13 },
            new InvalidOperationException("test"));

        _loggerSubstitute.Received(
            LogLevel.Warning,
            typeof(InvalidOperationException),
            "Warning from MqttClient (source): 'warning 13'.",
            4102);
    }

    [Fact]
    public void LogMqttClientInformation_ShouldLog()
    {
        _silverbackLogger.LogMqttClientInformation(
            "source",
            "info {0}",
            new object[] { "whatever" },
            null);

        _loggerSubstitute.Received(
            LogLevel.Information,
            null,
            "Information from MqttClient (source): 'info whatever'.",
            4103);
    }

    [Fact]
    public void LogMqttClientVerbose_ShouldLog()
    {
        _silverbackLogger.LogMqttClientVerbose(
            "source",
            "info {0}",
            new object[] { "whatever" },
            null);

        _loggerSubstitute.Received(
            LogLevel.Trace,
            null,
            "Verbose from MqttClient (source): 'info whatever'.",
            4104);
    }

    public void Dispose()
    {
        _mqttClientWrapper.Dispose();
        _consumer.Dispose();
        _producer.Dispose();
    }
}
