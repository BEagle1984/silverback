// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Broker.Mqtt;

public class DefaultMqttNetLoggerFixture
{
    [Fact]
    public void Publish_ShouldLogVerbose()
    {
        SilverbackLoggerSubstitute<DefaultMqttNetLogger> silverbackLogger = new();
        DefaultMqttNetLogger logger = new(silverbackLogger);

        logger.Publish(MqttNetLogLevel.Verbose, "source", "message {0} {1}", ["1", 2], new TestException());

        silverbackLogger.Received(
            LogLevel.Trace,
            typeof(TestException),
            "Verbose from MqttClient (source): 'message 1 2'.",
            4104);
    }

    [Fact]
    public void Publish_ShouldLogInfo()
    {
        SilverbackLoggerSubstitute<DefaultMqttNetLogger> silverbackLogger = new();
        DefaultMqttNetLogger logger = new(silverbackLogger);

        logger.Publish(MqttNetLogLevel.Info, "source", "message {0} {1}", ["1", 2], new TestException());

        silverbackLogger.Received(
            LogLevel.Information,
            typeof(TestException),
            "Information from MqttClient (source): 'message 1 2'.",
            4103);
    }

    [Fact]
    public void Publish_ShouldLogWarning()
    {
        SilverbackLoggerSubstitute<DefaultMqttNetLogger> silverbackLogger = new();
        DefaultMqttNetLogger logger = new(silverbackLogger);

        logger.Publish(MqttNetLogLevel.Warning, "source", "message {0} {1}", ["1", 2], new TestException());

        silverbackLogger.Received(
            LogLevel.Warning,
            typeof(TestException),
            "Warning from MqttClient (source): 'message 1 2'.",
            4102);
    }

    [Fact]
    public void Publish_ShouldLogError()
    {
        SilverbackLoggerSubstitute<DefaultMqttNetLogger> silverbackLogger = new();
        DefaultMqttNetLogger logger = new(silverbackLogger);

        logger.Publish(MqttNetLogLevel.Error, "source", "message {0} {1}", ["1", 2], new TestException());

        silverbackLogger.Received(
            LogLevel.Error,
            typeof(TestException),
            "Error from MqttClient (source): 'message 1 2'.",
            4101);
    }
}
