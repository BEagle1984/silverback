// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using NSubstitute;
using Silverback.Collections;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Diagnostics;

public class MqttLoggerExtensionsTests
{
    private readonly LoggerSubstitute<MqttLoggerExtensionsTests> _loggerSubstitute;

    private readonly ISilverbackLogger<MqttLoggerExtensionsTests> _silverbackLogger;

    private readonly IServiceProvider _serviceProvider;

    private readonly MqttConsumerConfiguration _consumerConfiguration = new()
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

    public MqttLoggerExtensionsTests()
    {
        _serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddLoggerSubstitute(LogLevel.Trace)
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMqtt()));

        _loggerSubstitute =
            (LoggerSubstitute<MqttLoggerExtensionsTests>)_serviceProvider
                .GetRequiredService<ILogger<MqttLoggerExtensionsTests>>();

        _silverbackLogger = _serviceProvider
            .GetRequiredService<ISilverbackLogger<MqttLoggerExtensionsTests>>();
    }

    [Fact]
    public void LogConsuming_Logged()
    {
        MqttConsumer consumer = (MqttConsumer)_serviceProvider.GetRequiredService<MqttBroker>()
            .AddConsumer(_consumerConfiguration);

        string expectedMessage =
            "Consuming message '123' from topic 'actual'. | " +
            $"consumerId: {consumer.Id}, endpointName: actual";

        _silverbackLogger.LogConsuming(
            new ConsumedApplicationMessage(
                new MqttApplicationMessage
                {
                    Topic = "actual",
                    UserProperties = new List<MqttUserProperty>
                    {
                        new(DefaultMessageHeaders.MessageId, "123")
                    }
                }),
            consumer);

        _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 4011);
    }

    [Fact]
    public void LogConnectError_Logged()
    {
        MqttClientWrapper mqttClientWrapper = new(
            Substitute.For<IMqttClient>(),
            new MqttClientConfiguration
            {
                ClientId = "test-client",
            ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "mqtt",
                        Port = 1234
                    }},
            Substitute.For<IBrokerCallbacksInvoker>(),
            _silverbackLogger);

        string expectedMessage =
            "Error occurred connecting to the MQTT broker. | clientId: test-client, broker: mqtt:1234";

        _silverbackLogger.LogConnectError(mqttClientWrapper, new MqttCommunicationException("test"));

        _loggerSubstitute.Received(
            LogLevel.Warning,
            typeof(MqttCommunicationException),
            expectedMessage,
            4021);
    }

    [Fact]
    public void LogConnectRetryError_Logged()
    {
        MqttClientWrapper mqttClientWrapper = new(
            Substitute.For<IMqttClient>(),
            new MqttClientConfiguration
            {
                ClientId = "test-client",
            ChannelOptions = new MqttClientWebSocketOptions
                    {
                        Uri = "mqtt"
                    }},
            Substitute.For<IBrokerCallbacksInvoker>(),
            _silverbackLogger);

        string expectedMessage =
            "Error occurred retrying to connect to the MQTT broker. | clientId: test-client, broker: mqtt";

        _silverbackLogger.LogConnectRetryError(mqttClientWrapper, new MqttCommunicationException("test"));

        _loggerSubstitute.Received(
            LogLevel.Debug,
            typeof(MqttCommunicationException),
            expectedMessage,
            4022);
    }

    [Fact]
    public void LogConnectionLost_Logged()
    {
        MqttClientWrapper mqttClientWrapper = new(
            Substitute.For<IMqttClient>(),
            new MqttClientConfiguration
            {
                ClientId = "test-client",
            ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "mqtt",
                        Port = 1234
                    }},
            Substitute.For<IBrokerCallbacksInvoker>(),
            _silverbackLogger);

        string expectedMessage =
            "Connection with the MQTT broker lost. The client will try to reconnect. | " +
            "clientId: test-client, broker: mqtt:1234";

        _silverbackLogger.LogConnectionLost(mqttClientWrapper);

        _loggerSubstitute.Received(
            LogLevel.Warning,
            null,
            expectedMessage,
            4023);
        }

        [Fact]
        public void LogReconnected_Logged()
        {
            var mqttClientWrapper = new MqttClientWrapper(
                Substitute.For<IMqttClient>(),
                new MqttClientConfig
                {
                    ClientId = "test-client",
                    ChannelOptions = new MqttClientWebSocketOptions
                    {
                        Uri = "mqtt"
                    }
                },
                Substitute.For<IBrokerCallbacksInvoker>(),
                _silverbackLogger);

            var expectedMessage =
                "Connection with the MQTT broker reestablished. | " +
                "clientId: test-client, broker: mqtt";

            _silverbackLogger.LogReconnected(mqttClientWrapper);

            _loggerSubstitute.Received(
                LogLevel.Information,
                null,
                expectedMessage,
                4024);
        }
    }
}
