// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Diagnostics;

public class KafkaTraceExtensionsTests
{
    private readonly LoggerSubstitute<KafkaTraceExtensionsTests> _loggerSubstitute;

    private readonly ISilverbackLogger<KafkaTraceExtensionsTests> _silverbackLogger;

    private readonly IConsumer _consumer;

    public KafkaTraceExtensionsTests()
    {
        _loggerSubstitute = new LoggerSubstitute<KafkaTraceExtensionsTests>(LogLevel.Trace);
        MappedLevelsLogger<KafkaTraceExtensionsTests> mappedLevelsLogger = new([], _loggerSubstitute);
        _silverbackLogger = new SilverbackLogger<KafkaTraceExtensionsTests>(mappedLevelsLogger);

        _consumer = Substitute.For<IConsumer>();
        _consumer.DisplayName.Returns("consumer1");
    }

    [Fact]
    public void LogOffsetStoring_ShouldLog()
    {
        _silverbackLogger.LogOffsetStoring(_consumer, new TopicPartitionOffset("topic1", 2, 42));

        _loggerSubstitute.Received(
            LogLevel.Trace,
            null,
            "Storing offset topic1[2]@42 | ConsumerName: consumer1",
            1999);
    }

    [Fact]
    public void LogConsumeLoopStarting_ShouldLog()
    {
        _silverbackLogger.LogConsumeLoopStarting(_consumer, "instance1", 7);

        _loggerSubstitute.Received(
            LogLevel.Trace,
            null,
            "Starting consume loop | InstanceId: instance1, TaskId: 7 | ConsumerName: consumer1",
            1999);
    }

    [Fact]
    public void LoggerMessages_ShouldShareTracingEvent() =>
        LoggerMessagesAssert.ShouldShareLogEvent(typeof(KafkaTraceExtensions), IntegrationLogEvents.Tracing);
}
