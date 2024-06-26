﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Diagnostics
{
    public class MqttLoggerExtensionsTests
    {
        private readonly LoggerSubstitute<MqttLoggerExtensionsTests> _loggerSubstitute;

        private readonly ISilverbackLogger<MqttLoggerExtensionsTests> _silverbackLogger;

        private readonly IServiceProvider _serviceProvider;

        private readonly MqttConsumerEndpoint _consumerEndpoint = new("test")
        {
            Configuration = new MqttClientConfig
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    RemoteEndpoint = new DnsEndPoint("test-server", 4242)
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
            var consumer = (MqttConsumer)_serviceProvider.GetRequiredService<MqttBroker>()
                .AddConsumer(_consumerEndpoint);

            var applicationMessage = new ConsumedApplicationMessage(
                new MqttApplicationMessageReceivedEventArgs(
                    "client1",
                    new MqttApplicationMessage
                    {
                        Topic = "actual",
                        UserProperties = new List<MqttUserProperty>
                        {
                            new(DefaultMessageHeaders.MessageId, "123")
                        }
                    },
                    new MqttPublishPacket(),
                    (_, _) => Task.CompletedTask));
            var expectedMessage =
                $"Consuming message '{applicationMessage.Id}' from topic 'actual'. | " +
                $"consumerId: {consumer.Id}, endpointName: actual";

            _silverbackLogger.LogConsuming(applicationMessage, consumer);

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 4011);
        }

        [Fact]
        public void LogConnectError_Logged()
        {
            var mqttClientWrapper = new MqttClientWrapper(
                Substitute.For<IMqttClient>(),
                new MqttClientConfig
                {
                    ClientId = "test-client",
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        RemoteEndpoint = new DnsEndPoint("mqtt", 1234)
                    }
                },
                Substitute.For<IBrokerCallbacksInvoker>(),
                _silverbackLogger);

            var expectedMessage =
                "Error occurred connecting to the MQTT broker. | clientId: test-client, broker: Unspecified/mqtt:1234";

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
            var mqttClientWrapper = new MqttClientWrapper(
                Substitute.For<IMqttClient>(),
                new MqttClientConfig
                {
                    ClientId = "test-client",
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        RemoteEndpoint = new DnsEndPoint("mqtt", 1234)
                    }
                },
                Substitute.For<IBrokerCallbacksInvoker>(),
                _silverbackLogger);

            var expectedMessage =
                "Connection with the MQTT broker lost. The client will try to reconnect. | " +
                "clientId: test-client, broker: Unspecified/mqtt:1234";

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
