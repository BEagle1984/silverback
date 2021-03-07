// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Diagnostics
{
    internal class InboundLogger
    {
        private readonly IBrokerLogEnricher _logEnricher;

        private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?>
            _processingInboundMessage;

        private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?>
            _errorProcessingInboundMessage;

        private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?>
            _consumerFatalError;

        private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?>
            _retryMessageProcessing;

        private readonly Action<ILogger, string, string, string?, string?, string?, string?, Exception?>
            _messageMoved;

        private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?>
            _messageSkipped;

        private readonly Action<ILogger, string, string, string?, string?, string?, string?, Exception?>
            _cannotMoveSequences;

        private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?>
            _rollbackToRetryFailed;

        private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?>
            _rollbackToSkipFailed;

        private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?>
            _nullMessageSkipped;

        private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?>
            _messageAlreadyProcessed;

        public InboundLogger(IBrokerLogEnricher logEnricher)
        {
            _logEnricher = Check.NotNull(logEnricher, nameof(logEnricher));

            _processingInboundMessage =
                SilverbackLoggerMessage.Define<string, string?, string?, string?, string?>(
                    EnrichLogEvent(IntegrationLogEvents.ProcessingInboundMessage));

            _errorProcessingInboundMessage =
                SilverbackLoggerMessage.Define<string, string?, string?, string?, string?>(
                    EnrichLogEvent(IntegrationLogEvents.ErrorProcessingInboundMessage));

            _consumerFatalError =
                SilverbackLoggerMessage.Define<string, string?, string?, string?, string?>(
                    EnrichLogEvent(IntegrationLogEvents.ConsumerFatalError));

            _retryMessageProcessing =
                SilverbackLoggerMessage.Define<string, string?, string?, string?, string?>(
                    EnrichLogEvent(IntegrationLogEvents.RetryMessageProcessing));

            _messageMoved =
                SilverbackLoggerMessage.Define<string, string, string?, string?, string?, string?>(
                    EnrichLogEvent(IntegrationLogEvents.MessageMoved));

            _messageSkipped =
                SilverbackLoggerMessage.Define<string, string?, string?, string?, string?>(
                    EnrichLogEvent(IntegrationLogEvents.MessageSkipped));

            _cannotMoveSequences =
                SilverbackLoggerMessage.Define<string, string, string?, string?, string?, string?>(
                    EnrichLogEvent(IntegrationLogEvents.CannotMoveSequences));

            _rollbackToRetryFailed =
                SilverbackLoggerMessage.Define<string, string?, string?, string?, string?>(
                    EnrichLogEvent(IntegrationLogEvents.RollbackToRetryFailed));

            _rollbackToSkipFailed =
                SilverbackLoggerMessage.Define<string, string?, string?, string?, string?>(
                    EnrichLogEvent(IntegrationLogEvents.RollbackToSkipFailed));

            _nullMessageSkipped =
                SilverbackLoggerMessage.Define<string, string?, string?, string?, string?>(
                    EnrichLogEvent(IntegrationLogEvents.NullMessageSkipped));

            _messageAlreadyProcessed =
                SilverbackLoggerMessage.Define<string, string?, string?, string?, string?>(
                    EnrichLogEvent(IntegrationLogEvents.MessageAlreadyProcessed));
        }

        public void LogProcessing(ISilverbackLogger logger, IRawInboundEnvelope envelope)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.ProcessingInboundMessage))
                return;

            (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
                envelope.Endpoint,
                envelope.Headers,
                envelope.BrokerMessageIdentifier);

            _processingInboundMessage.Invoke(
                logger.InnerLogger,
                envelope.ActualEndpointName,
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
                value1,
                value2,
                null);
        }

        public void LogProcessingError(
            ISilverbackLogger logger,
            IRawInboundEnvelope envelope,
            Exception exception)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.ErrorProcessingInboundMessage))
                return;

            (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
                envelope.Endpoint,
                envelope.Headers,
                envelope.BrokerMessageIdentifier);

            _errorProcessingInboundMessage.Invoke(
                logger.InnerLogger,
                envelope.ActualEndpointName,
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
            if (!logger.IsEnabled(IntegrationLogEvents.ConsumerFatalError))
                return;

            (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
                envelope.Endpoint,
                envelope.Headers,
                envelope.BrokerMessageIdentifier);

            _consumerFatalError.Invoke(
                logger.InnerLogger,
                envelope.ActualEndpointName,
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
                envelope.ActualEndpointName,
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
                value1,
                value2,
                null);
        }

        public void LogMoved(
            ISilverbackLogger logger,
            IRawInboundEnvelope envelope,
            IProducerEndpoint targetEndpoint)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.MessageMoved))
                return;

            (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
                envelope.Endpoint,
                envelope.Headers,
                envelope.BrokerMessageIdentifier);

            _messageMoved.Invoke(
                logger.InnerLogger,
                targetEndpoint.Name,
                envelope.ActualEndpointName,
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
                envelope.ActualEndpointName,
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
                value1,
                value2,
                null);
        }

        public void LogCannotMoveSequences(
            ISilverbackLogger logger,
            IRawInboundEnvelope envelope,
            ISequence sequence)
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
                envelope.ActualEndpointName,
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
                value1,
                value2,
                null);
        }

        public void LogRollbackToRetryFailed(
            ISilverbackLogger logger,
            IRawInboundEnvelope envelope,
            Exception exception)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.RollbackToRetryFailed))
                return;

            (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
                envelope.Endpoint,
                envelope.Headers,
                envelope.BrokerMessageIdentifier);

            _rollbackToRetryFailed.Invoke(
                logger.InnerLogger,
                envelope.ActualEndpointName,
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
                value1,
                value2,
                exception);
        }

        public void LogRollbackToSkipFailed(
            ISilverbackLogger logger,
            IRawInboundEnvelope envelope,
            Exception exception)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.RollbackToSkipFailed))
                return;

            (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
                envelope.Endpoint,
                envelope.Headers,
                envelope.BrokerMessageIdentifier);

            _rollbackToSkipFailed.Invoke(
                logger.InnerLogger,
                envelope.ActualEndpointName,
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
                value1,
                value2,
                exception);
        }

        public void LogNullMessageSkipped(ISilverbackLogger logger, IRawInboundEnvelope envelope)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.NullMessageSkipped))
                return;

            (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
                envelope.Endpoint,
                envelope.Headers,
                envelope.BrokerMessageIdentifier);

            _nullMessageSkipped.Invoke(
                logger.InnerLogger,
                envelope.ActualEndpointName,
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
                value1,
                value2,
                null);
        }

        public void LogAlreadyProcessed(ISilverbackLogger logger, IRawInboundEnvelope envelope)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.MessageAlreadyProcessed))
                return;

            (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
                envelope.Endpoint,
                envelope.Headers,
                envelope.BrokerMessageIdentifier);

            _messageAlreadyProcessed.Invoke(
                logger.InnerLogger,
                envelope.ActualEndpointName,
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
                value1,
                value2,
                null);
        }

        public void LogInboundTrace(
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

            var args = new List<object?>(argumentsProvider?.Invoke() ?? Array.Empty<object?>());
            args.Add(envelope.ActualEndpointName);
            args.Add(envelope.Headers.GetValue(DefaultMessageHeaders.MessageType));
            args.Add(envelope.Headers.GetValue(DefaultMessageHeaders.MessageId));
            args.Add(value1);
            args.Add(value2);

            logger.InnerLogger.Log(
                logLevel,
                eventId,
                exception,
                EnrichMessage(message),
                args.ToArray());
        }

        private LogEvent EnrichLogEvent(LogEvent logEvent)
        {
            var message = EnrichMessage(logEvent.Message);

            return new LogEvent(
                logEvent.Level,
                logEvent.EventId,
                message);
        }

        private string EnrichMessage(string message) =>
            $"{message} | " +
            "endpointName: {endpointName}, " +
            "messageType: {messageType}, " +
            "messageId: {messageId}, " +
            $"{_logEnricher.AdditionalPropertyName1}: {{{_logEnricher.AdditionalPropertyName1}}}, " +
            $"{_logEnricher.AdditionalPropertyName2}: {{{_logEnricher.AdditionalPropertyName2}}}";
    }
}
