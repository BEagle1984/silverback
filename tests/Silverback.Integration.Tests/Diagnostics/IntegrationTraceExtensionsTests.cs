// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Diagnostics;

public class IntegrationTraceExtensionsTests
{
    [Fact]
    public void LogSemaphoreAcquired_ShouldLog()
    {
        (LoggerSubstitute<IntegrationTraceExtensionsTests> logger, ISilverbackLogger silverbackLogger) = CreateLogger(LogLevel.Trace);

        silverbackLogger.LogSemaphoreAcquired();

        logger.Received(LogLevel.Trace, null, "Semaphore acquired", 1999);
    }

    [Fact]
    public void LogSequenceRemovedFromStore_ShouldLog()
    {
        (LoggerSubstitute<IntegrationTraceExtensionsTests> logger, ISilverbackLogger silverbackLogger) = CreateLogger(LogLevel.Trace);
        Guid storeId = Guid.NewGuid();

        silverbackLogger.LogSequenceRemovedFromStore("sequence1", storeId);

        logger.Received(LogLevel.Trace, null, $"Removing sequence 'sequence1' from store '{storeId}'.", 1999);
    }

    [Fact]
    public void LogConsumerStarting_ShouldLog()
    {
        (LoggerSubstitute<IntegrationTraceExtensionsTests> logger, ISilverbackLogger silverbackLogger) = CreateLogger(LogLevel.Trace);
        IConsumer consumer = Substitute.For<IConsumer>();
        consumer.DisplayName.Returns("consumer1");

        silverbackLogger.LogConsumerStarting(consumer);

        logger.Received(LogLevel.Trace, null, "Starting consumer... | ConsumerName: consumer1", 1999);
    }

    [Fact]
    public void LogConsumerTransactionCommitting_ShouldLog()
    {
        (LoggerSubstitute<IntegrationTraceExtensionsTests> logger, ISilverbackLogger silverbackLogger) = CreateLogger(LogLevel.Trace);

        silverbackLogger.LogConsumerTransactionCommitting(CreateEnvelope());

        logger.Received(
            LogLevel.Trace,
            null,
            "Committing consumer transaction... | EndpointName: test, BrokerMessageId: 42",
            1999);
    }

    [Fact]
    public void LogConsumerTransactionAborting_ShouldLog()
    {
        (LoggerSubstitute<IntegrationTraceExtensionsTests> logger, ISilverbackLogger silverbackLogger) = CreateLogger(LogLevel.Trace);

        silverbackLogger.LogConsumerTransactionAborting(CreateEnvelope(), new InvalidOperationException());

        logger.Received(
            LogLevel.Trace,
            typeof(InvalidOperationException),
            "Aborting consumer transaction... | EndpointName: test, BrokerMessageId: 42",
            1999);
    }

    [Fact]
    public void LogConsumerTransactionCommitting_ShouldNotEvaluateArguments_WhenTracingIsDisabled()
    {
        (_, ISilverbackLogger silverbackLogger) = CreateLogger(LogLevel.Debug);
        IRawInboundEnvelope envelope = Substitute.For<IRawInboundEnvelope>();

        silverbackLogger.LogConsumerTransactionCommitting(envelope);

        _ = envelope.DidNotReceive().BrokerMessageIdentifier;
    }

    [Fact]
    public void LogSemaphoreAcquired_ShouldApplyTracingLogLevelOverride()
    {
        (LoggerSubstitute<IntegrationTraceExtensionsTests> logger, ISilverbackLogger silverbackLogger) = CreateLogger(
            LogLevel.Information,
            levels => levels.SetLogLevel(IntegrationLogEvents.Tracing.EventId, LogLevel.Warning));

        silverbackLogger.LogSemaphoreAcquired();

        logger.Received(LogLevel.Warning, null, "Semaphore acquired", 1999);
    }

    [Fact]
    public void LoggerMessages_ShouldShareTracingEvent() =>
        LoggerMessagesAssert.ShouldShareLogEvent(typeof(IntegrationTraceExtensions), IntegrationLogEvents.Tracing);

    private static (LoggerSubstitute<IntegrationTraceExtensionsTests> Logger, ISilverbackLogger SilverbackLogger) CreateLogger(
        LogLevel minLevel,
        Action<LogLevelConfigurator>? configureLevels = null)
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddLoggerSubstitute(minLevel)
            .AddSilverback()
            .WithLogLevels(levels => configureLevels?.Invoke(levels))
            .WithConnectionToMessageBroker());

        return (
            (LoggerSubstitute<IntegrationTraceExtensionsTests>)serviceProvider.GetRequiredService<ILogger<IntegrationTraceExtensionsTests>>(),
            serviceProvider.GetRequiredService<ISilverbackLogger<IntegrationTraceExtensionsTests>>());
    }

    private static RawInboundEnvelope CreateEnvelope() =>
        new(
            Stream.Null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "42"));
}
