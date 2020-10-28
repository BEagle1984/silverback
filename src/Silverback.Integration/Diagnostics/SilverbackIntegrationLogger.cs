// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Diagnostics
{
    // TODO: Review and remove obsolete overloads (replace enumerable with sequence / base info on pipeline context)
    internal class SilverbackIntegrationLogger : ISilverbackIntegrationLogger
    {
        private readonly ISilverbackLogger _logger;

        public SilverbackIntegrationLogger(ISilverbackLogger logger, ILogTemplates logTemplates)
        {
            Check.NotNull(logger, nameof(logger));
            Check.NotNull(logTemplates, nameof(logTemplates));

            _logger = logger;
            LogTemplates = logTemplates;
        }

        public ILogTemplates LogTemplates { get; }

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter) =>
            _logger.Log(logLevel, eventId, state, exception, formatter);

        public void LogProcessing(ConsumerPipelineContext context)
        {
            Check.NotNull(context, nameof(context));

            LogInformationWithMessageInfo(
                IntegrationEventIds.ProcessingInboundMessage,
                "Processing inbound message.",
                context);
        }

        public void LogProcessingError(ConsumerPipelineContext context, Exception exception)
        {
            Check.NotNull(context, nameof(context));

            LogWarningWithMessageInfo(
                IntegrationEventIds.ErrorProcessingInboundMessage,
                exception,
                "Error occurred processing the inbound message.",
                context);
        }

        public void LogSequenceAborted(
            IRawInboundEnvelope envelope,
            ISequence sequence,
            SequenceAbortReason reason,
            Exception? exception)
        {
            switch (reason)
            {
                case SequenceAbortReason.Error:
                    LogWithMessageInfo(
                        LogLevel.Warning,
                        IntegrationEventIds.ErrorProcessingInboundMessage,
                        exception,
                        "Error occurred processing the inbound sequence of messages.",
                        envelope,
                        sequence);
                    break;
                case SequenceAbortReason.IncompleteSequence:
                    LogWithMessageInfo(
                        LogLevel.Warning,
                        IntegrationEventIds.IncompleteSequenceDiscarded,
                        null,
                        "Discarded incomplete sequence.",
                        envelope,
                        sequence);
                    break;
                case SequenceAbortReason.EnumerationAborted:
                case SequenceAbortReason.ConsumerAborted:
                case SequenceAbortReason.Disposing:
                    LogWithMessageInfo(
                        LogLevel.Debug,
                        IntegrationEventIds.SequenceProcessingAborted,
                        null,
                        $"Sequence processing has been aborted (reason: {reason}).",
                        envelope,
                        sequence);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
            }
        }

        public void LogTraceWithMessageInfo(
            EventId eventId,
            string logMessage,
            ConsumerPipelineContext context) =>
            LogWithMessageInfo(LogLevel.Trace, eventId, null, logMessage, context.Envelope, context.Sequence);

        public void LogTraceWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawOutboundEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Trace, eventId, null, logMessage, envelope);

        public void LogDebugWithMessageInfo(
            EventId eventId,
            string logMessage,
            ConsumerPipelineContext context) =>
            LogWithMessageInfo(LogLevel.Debug, eventId, null, logMessage, context.Envelope, context.Sequence);

        public void LogDebugWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawOutboundEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Debug, eventId, null, logMessage, envelope);

        public void LogInformationWithMessageInfo(
            EventId eventId,
            string logMessage,
            ConsumerPipelineContext context) =>
            LogWithMessageInfo(LogLevel.Information, eventId, null, logMessage, context.Envelope, context.Sequence);

        public void LogInformationWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawOutboundEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Information, eventId, null, logMessage, envelope);

        public void LogWarningWithMessageInfo(
            EventId eventId,
            string logMessage,
            ConsumerPipelineContext context) =>
            LogWithMessageInfo(LogLevel.Warning, eventId, null, logMessage, context.Envelope, context.Sequence);

        public void LogWarningWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawOutboundEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Warning, eventId, null, logMessage, envelope);

        public void LogWarningWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            ConsumerPipelineContext context) =>
            LogWithMessageInfo(LogLevel.Warning, eventId, exception, logMessage, context.Envelope, context.Sequence);

        public void LogWarningWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            IRawOutboundEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Warning, eventId, exception, logMessage, envelope);

        public void LogErrorWithMessageInfo(
            EventId eventId,
            string logMessage,
            ConsumerPipelineContext context) =>
            LogWithMessageInfo(LogLevel.Error, eventId, null, logMessage, context.Envelope, context.Sequence);

        public void LogErrorWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawOutboundEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Error, eventId, null, logMessage, envelope);

        public void LogErrorWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            ConsumerPipelineContext context) =>
            LogWithMessageInfo(LogLevel.Error, eventId, exception, logMessage, context.Envelope, context.Sequence);

        public void LogErrorWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            IRawOutboundEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Error, eventId, exception, logMessage, envelope);

        public void LogCriticalWithMessageInfo(
            EventId eventId,
            string logMessage,
            ConsumerPipelineContext context) =>
            LogWithMessageInfo(LogLevel.Critical, eventId, null, logMessage, context.Envelope, context.Sequence);

        public void LogCriticalWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawOutboundEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Critical, eventId, null, logMessage, envelope);

        public void LogCriticalWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            ConsumerPipelineContext context) =>
            LogWithMessageInfo(LogLevel.Critical, eventId, exception, logMessage, context.Envelope, context.Sequence);

        public void LogCriticalWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            IRawOutboundEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Critical, eventId, exception, logMessage, envelope);

        public void LogWithMessageInfo(
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            string logMessage,
            ConsumerPipelineContext context) =>
            LogWithMessageInfo(logLevel, eventId, exception, logMessage, context.Envelope, context.Sequence);

        public void LogWithMessageInfo(
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            string logMessage,
            IRawOutboundEnvelope envelope) =>
            LogWithMessageInfo(logLevel, eventId, exception, logMessage, envelope, null);

        public void LogWithMessageInfo(
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            string logMessage,
            IRawBrokerEnvelope envelope,
            ISequence? sequence = null)
        {
            if (!_logger.IsEnabled(logLevel))
                return;

            Check.NotEmpty(logMessage, nameof(logMessage));

            var arguments = GetLogArguments(envelope, sequence, ref logMessage);

            if (exception != null)
                _logger.Log(logLevel, eventId, exception, logMessage, arguments);
            else
                _logger.Log(logLevel, eventId, logMessage, arguments);
        }

        private object?[] GetLogArguments(IRawBrokerEnvelope envelope, ISequence? sequence, ref string logMessage)
        {
            if (envelope == null)
                return Array.Empty<object>();

            if (envelope is IRawInboundEnvelope inboundEnvelope)
            {
                return sequence == null
                    ? GetInboundLogArguments(inboundEnvelope, ref logMessage)
                    : GetInboundSequenceLogArguments(inboundEnvelope, sequence, ref logMessage);
            }

            return GetOutboundLogArguments(envelope, ref logMessage);
        }

        private object?[] GetInboundLogArguments(IRawInboundEnvelope envelope, ref string logMessage)
        {
            logMessage += LogTemplates.GetInboundMessageLogTemplate(envelope.Endpoint);

            var arguments = new List<object?>
            {
                envelope.ActualEndpointName,
                envelope.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId)
            };

            foreach (string key in LogTemplates.GetInboundMessageArguments(envelope.Endpoint))
            {
                arguments.Add(envelope.AdditionalLogData.TryGetValue(key, out string value) ? value : null);
            }

            return arguments.ToArray();
        }

        private object?[] GetInboundSequenceLogArguments(
            IRawInboundEnvelope envelope,
            ISequence sequence,
            ref string logMessage)
        {
            logMessage += LogTemplates.GetInboundSequenceLogTemplate(envelope.Endpoint);

            var arguments = new List<object?>
            {
                envelope.ActualEndpointName,
                envelope.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
                sequence.GetType().Name,
                sequence.SequenceId,
                sequence.Length,
                sequence.IsNew,
                sequence.IsComplete || sequence.IsCompleting
            };

            foreach (string key in LogTemplates.GetInboundMessageArguments(envelope.Endpoint))
            {
                arguments.Add(envelope.AdditionalLogData.TryGetValue(key, out string value) ? value : null);
            }

            return arguments.ToArray();
        }

        private object?[] GetOutboundLogArguments(IRawBrokerEnvelope envelope, ref string logMessage)
        {
            logMessage += LogTemplates.GetOutboundMessageLogTemplate(envelope.Endpoint);

            var arguments = new List<object?>
            {
                envelope.Endpoint.Name,
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId)
            };

            foreach (string key in LogTemplates.GetOutboundMessageArguments(envelope.Endpoint))
            {
                arguments.Add(envelope.AdditionalLogData.TryGetValue(key, out string value) ? value : null);
            }

            return arguments.ToArray();
        }
    }
}
