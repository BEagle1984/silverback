// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Diagnostics;

internal sealed class ConsumerLogger
{
    private readonly IBrokerLogEnricher _logEnricher;

    private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?> _processing;

    private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?> _processingError;

    private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?> _processingFatalError;

    private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?> _retryMessageProcessing;

    private readonly Action<ILogger, string, string, string?, string?, string?, string?, Exception?> _messageMoved;

    private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?> _messageSkipped;

    private readonly Action<ILogger, string, string, string?, string?, string?, string?, Exception?> _cannotMoveSequences;

    private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?> _rollbackToRetryFailed;

    private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?> _rollbackToSkipFailed;

    private readonly Action<ILogger, string, string, string?, string?, string?, string?, Exception?> _invalidMessageConsumed;

    public ConsumerLogger(IBrokerLogEnricher logEnricher)
    {
        _logEnricher = Check.NotNull(logEnricher, nameof(logEnricher));

        _processing = _logEnricher.Define(IntegrationLogEvents.ProcessingConsumedMessage);
        _processingError = _logEnricher.Define(IntegrationLogEvents.ProcessingConsumedMessageError);
        _processingFatalError = _logEnricher.Define(IntegrationLogEvents.ProcessingConsumedMessageFatalError);
        _retryMessageProcessing = _logEnricher.Define(IntegrationLogEvents.RetryMessageProcessing);
        _messageMoved = _logEnricher.Define<string>(IntegrationLogEvents.MessageMoved);
        _messageSkipped = _logEnricher.Define(IntegrationLogEvents.MessageSkipped);
        _cannotMoveSequences = _logEnricher.Define<string>(IntegrationLogEvents.CannotMoveSequences);
        _rollbackToRetryFailed = _logEnricher.Define(IntegrationLogEvents.RollbackToRetryFailed);
        _rollbackToSkipFailed = _logEnricher.Define(IntegrationLogEvents.RollbackToSkipFailed);
        _invalidMessageConsumed = _logEnricher.Define<string>(IntegrationLogEvents.InvalidMessageConsumed);
    }

    public void LogProcessing(ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.ProcessingConsumedMessage))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
            envelope.Endpoint,
            envelope.Headers,
            envelope.BrokerMessageIdentifier);

        _processing.Invoke(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            null);
    }

    public void LogProcessingError(ISilverbackLogger logger, IRawInboundEnvelope envelope, Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.ProcessingConsumedMessageError))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
            envelope.Endpoint,
            envelope.Headers,
            envelope.BrokerMessageIdentifier);

        _processingError.Invoke(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            exception);
    }

    public void LogProcessingFatalError(
        ISilverbackLogger logger,
        IRawInboundEnvelope envelope,
        Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.ProcessingConsumedMessageFatalError))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
            envelope.Endpoint,
            envelope.Headers,
            envelope.BrokerMessageIdentifier);

        _processingFatalError.Invoke(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            exception);
    }

    public void LogRetryProcessing(ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.RetryMessageProcessing))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
            envelope.Endpoint,
            envelope.Headers,
            envelope.BrokerMessageIdentifier);

        _retryMessageProcessing.Invoke(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            null);
    }

    public void LogMoved(ISilverbackLogger logger, IRawInboundEnvelope envelope, ProducerEndpointConfiguration endpointConfiguration)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.MessageMoved))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
            envelope.Endpoint,
            envelope.Headers,
            envelope.BrokerMessageIdentifier);

        _messageMoved.Invoke(
            logger.InnerLogger,
            endpointConfiguration.DisplayName,
            envelope.Endpoint.DisplayName,
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            null);
    }

    public void LogSkipped(ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.MessageSkipped))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
            envelope.Endpoint,
            envelope.Headers,
            envelope.BrokerMessageIdentifier);

        _messageSkipped.Invoke(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            null);
    }

    public void LogCannotMoveSequences(ISilverbackLogger logger, IRawInboundEnvelope envelope, ISequence sequence)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.CannotMoveSequences))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
            envelope.Endpoint,
            envelope.Headers,
            envelope.BrokerMessageIdentifier);

        _cannotMoveSequences.Invoke(
            logger.InnerLogger,
            sequence.GetType().Name,
            envelope.Endpoint.DisplayName,
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            null);
    }

    public void LogRollbackToRetryFailed(ISilverbackLogger logger, IRawInboundEnvelope envelope, Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.RollbackToRetryFailed))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
            envelope.Endpoint,
            envelope.Headers,
            envelope.BrokerMessageIdentifier);

        _rollbackToRetryFailed.Invoke(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            exception);
    }

    public void LogRollbackToSkipFailed(ISilverbackLogger logger, IRawInboundEnvelope envelope, Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.RollbackToSkipFailed))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
            envelope.Endpoint,
            envelope.Headers,
            envelope.BrokerMessageIdentifier);

        _rollbackToSkipFailed.Invoke(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            exception);
    }

    public void LogInvalidMessage(ISilverbackLogger logger, IRawInboundEnvelope envelope, string validationErrors)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.InvalidMessageConsumed))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
            envelope.Endpoint,
            envelope.Headers,
            envelope.BrokerMessageIdentifier);

        _invalidMessageConsumed.Invoke(
            logger.InnerLogger,
            validationErrors,
            envelope.Endpoint.DisplayName,
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            null);
    }

    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem", Justification = "Optimized via IsEnabled")]
    [SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Optimized via IsEnabled")]
    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Optimized via IsEnabled")]
    public void LogConsumerTrace(
        ISilverbackLogger logger,
        LogLevel logLevel,
        EventId eventId,
        string message,
        IRawInboundEnvelope envelope,
        Exception? exception,
        Func<object?[]>? argumentsProvider = null)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.LowLevelTracing))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
            envelope.Endpoint,
            envelope.Headers,
            envelope.BrokerMessageIdentifier);

        object?[] args =
        [
            ..argumentsProvider?.Invoke() ?? [],
            envelope.Endpoint.DisplayName,
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2
        ];

        logger.InnerLogger.Log(
            logLevel,
            eventId,
            exception,
            _logEnricher.Enrich(message),
            args);
    }
}
