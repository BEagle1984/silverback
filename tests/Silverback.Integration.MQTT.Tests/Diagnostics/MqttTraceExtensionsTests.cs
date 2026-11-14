// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Diagnostics;

public class MqttTraceExtensionsTests
{
    private readonly LoggerSubstitute<MqttTraceExtensionsTests> _loggerSubstitute;

    private readonly ISilverbackLogger<MqttTraceExtensionsTests> _silverbackLogger;

    private readonly IConsumer _consumer;

    public MqttTraceExtensionsTests()
    {
        _loggerSubstitute = new LoggerSubstitute<MqttTraceExtensionsTests>(LogLevel.Trace);
        MappedLevelsLogger<MqttTraceExtensionsTests> mappedLevelsLogger = new([], _loggerSubstitute);
        _silverbackLogger = new SilverbackLogger<MqttTraceExtensionsTests>(mappedLevelsLogger);

        _consumer = Substitute.For<IConsumer>();
        _consumer.DisplayName.Returns("consumer1");
    }

    [Fact]
    public void LogRollbackRestartSkipped_ShouldLog()
    {
        _silverbackLogger.LogRollbackRestartSkipped(_consumer, new ConsumerChannel(1, 2, _silverbackLogger));

        _loggerSubstitute.Received(
            LogLevel.Trace,
            null,
            "Skipping rollback restart of channel 2 (taken over by the stop) | ConsumerName: consumer1",
            1999);
    }

    [Fact]
    public void LoggerMessages_ShouldShareTracingEvent() =>
        LoggerMessagesAssert.ShouldShareLogEvent(typeof(MqttTraceExtensions), IntegrationLogEvents.Tracing);
}
