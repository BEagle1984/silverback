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
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
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

        private readonly IServiceProvider? _serviceProvider;

        private readonly MqttConsumerEndpoint _consumerEndpoint = new("test")
        {
            Configuration = new MqttClientConfig
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
            var consumer = (MqttConsumer)_serviceProvider.GetRequiredService<MqttBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
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
            var mqttClientWrapper = new MqttClientWrapper(
                Substitute.For<IMqttClient>(),
                new MqttClientConfig
                {
                    ClientId = "test-client"
                },
                _silverbackLogger);

            var expectedMessage =
                "Error occurred connecting to the MQTT broker. | clientId: test-client";

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
                    ClientId = "test-client"
                },
                _silverbackLogger);

            var expectedMessage =
                "Error occurred connecting to the MQTT broker. | clientId: test-client";

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
                    ClientId = "test-client"
                },
                _silverbackLogger);

            var expectedMessage =
                "Connection with the MQTT broker lost. The client will try to reconnect. | " +
                "clientId: test-client";

            _silverbackLogger.LogConnectionLost(mqttClientWrapper);

            _loggerSubstitute.Received(
                LogLevel.Warning,
                null,
                expectedMessage,
                4023);
        }
    }
}
