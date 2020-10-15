// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly ILogTemplates _logTemplates;

        public SilverbackIntegrationLogger(ISilverbackLogger logger, ILogTemplates logTemplates)
        {
            Check.NotNull(logger, nameof(logger));
            Check.NotNull(logTemplates, nameof(logTemplates));

            _logger = logger;
            _logTemplates = logTemplates;
        }

        public ILogTemplates LogTemplates => _logTemplates;

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter) =>
            _logger.Log(logLevel, eventId, state, exception, formatter);

        // TODO: Invoke
        // TODO: Change from envelopes to Producer-/ConsumerPipelineContext ;-)
        public void LogProcessing(IRawBrokerEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            // var message = envelopes.Count > 1
            //     ? $"Processing the batch of {envelopes.Count} inbound messages."
            //     : "Processing inbound message.";

            // TODO: Log sequence

            LogInformationWithMessageInfo(
                IntegrationEventIds.ProcessingInboundMessage,
                "Processing inbound message.",
                envelope);
        }

        // TODO: Invoke
        // TODO: Change from envelopes to Producer-/ConsumerPipelineContext ;-)
        public void LogProcessingError(IRawBrokerEnvelope envelope, Exception exception)
        {
            Check.NotNull(envelope, nameof(envelope));

            // TODO: Log sequence

            // var message = envelopes.Count > 1
            //     ? $"Error occurred processing the batch of {envelopes.Count} inbound messages."
            //     : "Error occurred processing the inbound message.";

            LogWarningWithMessageInfo(
                IntegrationEventIds.ErrorProcessingInboundMessage,
                exception,
                "Error occurred processing the inbound message.",
                envelope);
        }

        public void LogSequenceAborted(
            ConsumerPipelineContext context,
            SequenceAbortReason reason,
            Exception? abortException)
        {
            if (reason == SequenceAbortReason.Error && abortException != null)
            {
                // TODO: Log sequence details
                LogWarningWithMessageInfo(
                    IntegrationEventIds.ErrorProcessingInboundMessage,
                    abortException,
                    "Error occurred processing the inbound sequence of messages.",
                    context.Envelope);
            }
            else
            {
                context.Envelope.AdditionalLogData["abortReason"] = reason.ToString();

                LogWarningWithMessageInfo(
                    IntegrationEventIds.SequenceProcessingAborted,
                    "Sequence processing has been aborted.",
                    context.Envelope);
            }
        }

        public void LogTraceWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Trace, eventId, null, logMessage, new[] { envelope });

        public void LogTraceWithMessageInfo(
            EventId eventId,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            LogWithMessageInfo(LogLevel.Trace, eventId, null, logMessage, envelopes);

        public void LogDebugWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Debug, eventId, null, logMessage, new[] { envelope });

        public void LogDebugWithMessageInfo(
            EventId eventId,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            LogWithMessageInfo(LogLevel.Debug, eventId, null, logMessage, envelopes);

        public void LogInformationWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Information, eventId, null, logMessage, new[] { envelope });

        public void LogInformationWithMessageInfo(
            EventId eventId,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            LogWithMessageInfo(LogLevel.Information, eventId, null, logMessage, envelopes);

        public void LogWarningWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Warning, eventId, null, logMessage, new[] { envelope });

        public void LogWarningWithMessageInfo(
            EventId eventId,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            LogWithMessageInfo(LogLevel.Warning, eventId, null, logMessage, envelopes);

        public void LogWarningWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Warning, eventId, exception, logMessage, new[] { envelope });

        public void LogWarningWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            LogWithMessageInfo(LogLevel.Warning, eventId, exception, logMessage, envelopes);

        public void LogErrorWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Error, eventId, null, logMessage, new[] { envelope });

        public void LogErrorWithMessageInfo(
            EventId eventId,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            LogWithMessageInfo(LogLevel.Error, eventId, null, logMessage, envelopes);

        public void LogErrorWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Error, eventId, exception, logMessage, new[] { envelope });

        public void LogErrorWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            LogWithMessageInfo(LogLevel.Error, eventId, exception, logMessage, envelopes);

        public void LogCriticalWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Critical, eventId, null, logMessage, new[] { envelope });

        public void LogCriticalWithMessageInfo(
            EventId eventId,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            LogWithMessageInfo(LogLevel.Critical, eventId, null, logMessage, envelopes);

        public void LogCriticalWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            LogWithMessageInfo(LogLevel.Critical, eventId, exception, logMessage, new[] { envelope });

        public void LogCriticalWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            LogWithMessageInfo(LogLevel.Critical, eventId, exception, logMessage, envelopes);

        public void LogWithMessageInfo(
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            LogWithMessageInfo(logLevel, eventId, exception, logMessage, new[] { envelope });

        public void LogWithMessageInfo(
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes)
        {
            if (!_logger.IsEnabled(logLevel))
                return;

            Check.NotEmpty(logMessage, nameof(logMessage));
            Check.NotEmpty(envelopes, nameof(envelopes));

            var arguments = GetLogArguments(envelopes, ref logMessage);

            if (exception != null)
                _logger.Log(logLevel, eventId, exception, logMessage, arguments);
            else
                _logger.Log(logLevel, eventId, logMessage, arguments);
        }

        private object?[] GetLogArguments(
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes,
            ref string logMessage)
        {
            var firstEnvelope = envelopes.FirstOrDefault();

            if (firstEnvelope == null)
                return Array.Empty<object>();

            if (firstEnvelope is IRawInboundEnvelope inboundEnvelope)
            {
                return envelopes.Count == 1 && !firstEnvelope.Headers.Contains(DefaultMessageHeaders.BatchId)
                    ? GetInboundLogArguments(inboundEnvelope, ref logMessage)
                    : GetInboundBatchLogArguments(inboundEnvelope, ref logMessage);
            }

            return envelopes.Count == 1
                ? GetOutboundLogArguments(firstEnvelope, ref logMessage)
                : GetOutboundBatchLogArguments(firstEnvelope, ref logMessage);
        }

        private object?[] GetInboundLogArguments(IRawInboundEnvelope envelope, ref string logMessage)
        {
            logMessage += _logTemplates.GetInboundMessageLogTemplate(envelope.Endpoint);

            var arguments = new List<object?>
            {
                envelope.ActualEndpointName,
                envelope.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId)
            };

            foreach (string key in _logTemplates.GetInboundMessageArguments(envelope.Endpoint))
            {
                arguments.Add(envelope.AdditionalLogData.TryGetValue(key, out string value) ? value : null);
            }

            return arguments.ToArray();
        }

        private object?[] GetInboundBatchLogArguments(IRawInboundEnvelope envelope, ref string logMessage)
        {
            logMessage += _logTemplates.GetInboundBatchLogTemplate(envelope.Endpoint);

            return new object?[]
            {
                envelope.ActualEndpointName,
                envelope.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts),
                envelope.Headers.GetValue(DefaultMessageHeaders.BatchId),
                envelope.Headers.GetValue(DefaultMessageHeaders.BatchSize)
            };
        }

        private object?[] GetOutboundLogArguments(IRawBrokerEnvelope envelope, ref string logMessage)
        {
            logMessage += _logTemplates.GetOutboundMessageLogTemplate(envelope.Endpoint);

            var arguments = new List<object?>
            {
                envelope.Endpoint?.Name,
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId)
            };

            foreach (string key in _logTemplates.GetOutboundMessageArguments(envelope.Endpoint))
            {
                arguments.Add(envelope.AdditionalLogData.TryGetValue(key, out string value) ? value : null);
            }

            return arguments.ToArray();
        }

        private object?[] GetOutboundBatchLogArguments(IRawBrokerEnvelope envelope, ref string logMessage)
        {
            logMessage += _logTemplates.GetOutboundBatchLogTemplate(envelope.Endpoint);

            return new object?[]
            {
                envelope.Endpoint?.Name
            };
        }
    }
}
