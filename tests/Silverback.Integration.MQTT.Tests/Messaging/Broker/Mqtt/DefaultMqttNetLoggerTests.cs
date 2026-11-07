// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet.Diagnostics.Logger;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Broker.Mqtt;

public class DefaultMqttNetLoggerTests
{
    public static TheoryData<LogEvent> MqttClientLogEvents =>
    [
        MqttLogEvents.MqttClientLogError,
        MqttLogEvents.MqttClientLogWarning,
        MqttLogEvents.MqttClientLogInformation,
        MqttLogEvents.MqttClientLogVerbose
    ];

    [Fact]
    public void IsEnabled_ShouldReturnFalse_WhenNoEventIsEnabled()
    {
        DefaultMqttNetLogger logger = new(new TestSilverbackLogger());

        bool result = logger.IsEnabled;

        result.ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(MqttClientLogEvents))]
    public void IsEnabled_ShouldReturnTrue_WhenAnyEventIsEnabled(LogEvent logEvent)
    {
        DefaultMqttNetLogger logger = new(new TestSilverbackLogger(logEvent));

        bool result = logger.IsEnabled;

        result.ShouldBeTrue();
    }

    [Fact]
    public void Publish_ShouldLogVerbose()
    {
        SilverbackLoggerSubstitute<DefaultMqttNetLogger> silverbackLogger = new();
        DefaultMqttNetLogger logger = new(silverbackLogger);

        logger.Publish(MqttNetLogLevel.Verbose, "source", "message {0} {1}", ["1", 2], new TestException());

        silverbackLogger.Received(
            LogLevel.Trace,
            typeof(TestException),
            "Verbose from MqttClient (source): 'message 1 2'",
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
            "Information from MqttClient (source): 'message 1 2'",
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
            "Warning from MqttClient (source): 'message 1 2'",
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
            "Error from MqttClient (source): 'message 1 2'",
            4101);
    }

    private sealed class TestSilverbackLogger : ISilverbackLogger<DefaultMqttNetLogger>
    {
        private readonly LogEvent? _enabledLogEvent;

        public TestSilverbackLogger(LogEvent? enabledLogEvent = null)
        {
            _enabledLogEvent = enabledLogEvent;
        }

        public ILogger InnerLogger => NullLogger.Instance;

        public bool IsEnabled(LogEvent logEvent) => logEvent == _enabledLogEvent;
    }
}
