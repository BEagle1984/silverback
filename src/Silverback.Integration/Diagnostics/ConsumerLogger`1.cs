// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Diagnostics;

internal sealed class ConsumerLogger<TCategoryName> : SilverbackLogger<TCategoryName>, IConsumerLogger<TCategoryName>
{
    private readonly InternalConsumerLoggerFactory _loggerFactory;

    public ConsumerLogger(IMappedLevelsLogger<TCategoryName> mappedLevelsLogger, InternalConsumerLoggerFactory loggerFactory)
        : base(mappedLevelsLogger)
    {
        _loggerFactory = Check.NotNull(loggerFactory, nameof(loggerFactory));
    }

    public void LogProcessing(IRawInboundEnvelope envelope) =>
        _loggerFactory.GetConsumerLogger(envelope.Endpoint.Configuration).LogProcessing(this, envelope);

    public void LogProcessingError(IRawInboundEnvelope envelope, Exception exception) =>
        _loggerFactory.GetConsumerLogger(envelope.Endpoint.Configuration).LogProcessingError(this, envelope, exception);

    public void LogProcessingFatalError(IRawInboundEnvelope envelope, Exception exception) =>
        _loggerFactory.GetConsumerLogger(envelope.Endpoint.Configuration).LogProcessingFatalError(this, envelope, exception);

    public void LogRetryProcessing(IRawInboundEnvelope envelope) =>
        _loggerFactory.GetConsumerLogger(envelope.Endpoint.Configuration).LogRetryProcessing(this, envelope);

    public void LogMoved(IRawInboundEnvelope envelope, ProducerEndpointConfiguration endpointConfiguration) =>
        _loggerFactory.GetConsumerLogger(envelope.Endpoint.Configuration).LogMoved(this, envelope, endpointConfiguration);

    public void LogSkipped(IRawInboundEnvelope envelope) =>
        _loggerFactory.GetConsumerLogger(envelope.Endpoint.Configuration).LogSkipped(this, envelope);

    public void LogCannotMoveSequences(IRawInboundEnvelope envelope, ISequence sequence) =>
        _loggerFactory.GetConsumerLogger(envelope.Endpoint.Configuration).LogCannotMoveSequences(this, envelope, sequence);

    public void LogRollbackToRetryFailed(IRawInboundEnvelope envelope, Exception exception) =>
        _loggerFactory.GetConsumerLogger(envelope.Endpoint.Configuration).LogRollbackToRetryFailed(this, envelope, exception);

    public void LogRollbackToSkipFailed(IRawInboundEnvelope envelope, Exception exception) =>
        _loggerFactory.GetConsumerLogger(envelope.Endpoint.Configuration).LogRollbackToSkipFailed(this, envelope, exception);

    public void LogInvalidMessage(IRawInboundEnvelope envelope, string validationErrors) =>
        _loggerFactory.GetConsumerLogger(envelope.Endpoint.Configuration).LogInvalidMessage(this, envelope, validationErrors);

    public void LogConsumerTrace(LogEvent logEvent, IRawInboundEnvelope envelope, Func<object?[]>? argumentsProvider = null) =>
        LogConsumerTrace(logEvent, envelope, null, argumentsProvider);

    public void LogConsumerTrace(
        LogEvent logEvent,
        IRawInboundEnvelope envelope,
        Exception? exception,
        Func<object?[]>? argumentsProvider = null)
    {
        if (logEvent.Level > LogLevel.Trace)
            throw new InvalidOperationException("This method is intended for tracing only.");

        _loggerFactory.GetConsumerLogger(envelope.Endpoint.Configuration).LogConsumerTrace(
            this,
            logEvent.Level,
            logEvent.EventId,
            logEvent.Message,
            envelope,
            exception,
            argumentsProvider);
    }

    public void LogConsumerLowLevelTrace(
        string message,
        IRawInboundEnvelope envelope,
        Func<object?[]>? argumentsProvider = null) =>
        LogConsumerLowLevelTrace(message, envelope, null, argumentsProvider);

    public void LogConsumerLowLevelTrace(
        string message,
        IRawInboundEnvelope envelope,
        Exception? exception,
        Func<object?[]>? argumentsProvider = null) =>
        _loggerFactory.GetConsumerLogger(envelope.Endpoint.Configuration).LogConsumerTrace(
            this,
            IntegrationLogEvents.LowLevelTracing.Level,
            IntegrationLogEvents.LowLevelTracing.EventId,
            message,
            envelope,
            exception,
            argumentsProvider);
}
