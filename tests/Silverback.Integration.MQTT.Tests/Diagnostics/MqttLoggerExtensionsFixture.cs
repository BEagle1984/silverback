// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
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
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Logging;
using Xunit;

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
        MappedLevelsLogger<MqttLoggerExtensionsFixture> mappedLevelsLogger = new([], _loggerSubstitute);
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
                    RemoteEndpoint = new DnsEndPoint("test-broker", 1234)
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
            Substitute.For<ISilverbackLogger<MqttConsumer>>());

        _producer = new MqttProducer(
            "producer1",
            _mqttClientWrapper,
            new MqttClientConfiguration
            {
                ProducerEndpoints = new ValueReadOnlyCollection<MqttProducerEndpointConfiguration>(
                [
                    new MqttProducerEndpointConfiguration
                    {
                        EndpointResolver = new MqttStaticProducerEndpointResolver("topic1")
                    }
                ])
            },
            Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>(),
            serviceProvider,
            Substitute.For<ISilverbackLogger<MqttProducer>>());
    }

    [Fact]
    public void LogConsuming_ShouldLog()
    {
        ConsumedApplicationMessage applicationMessage = new(
            new MqttApplicationMessageReceivedEventArgs(
                "client1",
                new MqttApplicationMessage
                {
                    Topic = "some-topic"
                },
                new MqttPublishPacket(),
                (_, _) => Task.CompletedTask));

        _silverbackLogger.LogConsuming(applicationMessage, _consumer);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            $"Consuming message {applicationMessage.Id} from topic some-topic | ConsumerName: consumer1",
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
                    Topic = "some-topic"
                },
                new MqttPublishPacket(),
                (_, _) => Task.CompletedTask));

        _silverbackLogger.LogAcknowledgeFailed(applicationMessage, _consumer, new TimeoutException());

        _loggerSubstitute.Received(
            LogLevel.Warning,
            typeof(TimeoutException),
            $"Failed to acknowledge message {applicationMessage.Id} from topic some-topic | ConsumerName: consumer1",
            4012);
    }

    [Fact]
    public void LogNoMatchingSubscribers_ShouldLog()
    {
        _silverbackLogger.LogNoMatchingSubscribers("my/topic");

        _loggerSubstitute.Received(
            LogLevel.Warning,
            null,
            "No matching subscribers found for the produced message | Topic: my/topic",
            4013);
    }

    [Fact]
    public void LogConnectError_ShouldLog()
    {
        _silverbackLogger.LogConnectError(_mqttClientWrapper, new MqttCommunicationException("test"));

        _loggerSubstitute.Received(
            LogLevel.Warning,
            typeof(MqttCommunicationException),
            "Error occurred connecting to MQTT | ClientName: client1, ClientId: id1, Broker: Unspecified/test-broker:1234",
            4021);
    }

    [Fact]
    public void LogConnectRetryError_ShouldLog()
    {
        _silverbackLogger.LogConnectRetryError(_mqttClientWrapper, new MqttCommunicationException("test"));

        _loggerSubstitute.Received(
            LogLevel.Debug,
            typeof(MqttCommunicationException),
            "Error occurred retrying to connect to MQTT | ClientName: client1, ClientId: id1, Broker: Unspecified/test-broker:1234",
            4022);
    }

    [Fact]
    public void LogConnectionLost_ShouldLog()
    {
        _silverbackLogger.LogConnectionLost(_mqttClientWrapper);

        _loggerSubstitute.Received(
            LogLevel.Warning,
            null,
            "Connection to MQTT lost; the client will try to reconnect | ClientName: client1, ClientId: id1, Broker: Unspecified/test-broker:1234",
            4023);
    }

    [Fact]
    public void LogReconnected_ShouldLog()
    {
        _silverbackLogger.LogReconnected(_mqttClientWrapper);

        _loggerSubstitute.Received(
            LogLevel.Information,
            null,
            "Connection to MQTT reestablished | ClientName: client1, ClientId: id1, Broker: Unspecified/test-broker:1234",
            4024);
    }

    [Fact]
    public void LogProducerQueueProcessingCanceled_ShouldLog()
    {
        _silverbackLogger.LogProducerQueueProcessingCanceled(_mqttClientWrapper);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Producer queue processing canceled | ClientName: client1, ClientId: id1",
            4031);
    }

    [Fact]
    public void LogConsumerSubscribed_ShouldLog()
    {
        _silverbackLogger.LogConsumerSubscribed("my/topic/#", _consumer);

        _loggerSubstitute.Received(
            LogLevel.Information,
            null,
            "Consumer subscribed to my/topic/# | ClientName: client1, ClientId: id1, ConsumerName: consumer1",
            4041);
    }

    [Fact]
    public void LogMqttClientError_ShouldLog()
    {
        _silverbackLogger.LogMqttClientError(
            "source",
            "error {0} occurred",
            [42],
            new InvalidOperationException("test"));

        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(InvalidOperationException),
            "Error from MqttClient (source): 'error 42 occurred'",
            4101);
    }

    [Fact]
    public void LogMqttClientWarning_ShouldLog()
    {
        _silverbackLogger.LogMqttClientWarning(
            "source",
            "warning {0}",
            [13],
            new InvalidOperationException("test"));

        _loggerSubstitute.Received(
            LogLevel.Warning,
            typeof(InvalidOperationException),
            "Warning from MqttClient (source): 'warning 13'",
            4102);
    }

    [Fact]
    public void LogMqttClientInformation_ShouldLog()
    {
        _silverbackLogger.LogMqttClientInformation(
            "source",
            "info {0}",
            ["whatever"],
            null);

        _loggerSubstitute.Received(
            LogLevel.Information,
            null,
            "Information from MqttClient (source): 'info whatever'",
            4103);
    }

    [Fact]
    public void LogMqttClientVerbose_ShouldLog()
    {
        _silverbackLogger.LogMqttClientVerbose(
            "source",
            "info {0}",
            ["whatever"],
            null);

        _loggerSubstitute.Received(
            LogLevel.Trace,
            null,
            "Verbose from MqttClient (source): 'info whatever'",
            4104);
    }

    public void Dispose()
    {
        _mqttClientWrapper.Dispose();
        _consumer.Dispose();
        _producer.Dispose();
    }
}
