// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Silverback.Messaging.Messages
{
    // TODO: Test
    public class MessageLogger
    {
        #region Generic

        public void LogTrace(ILogger logger, string logMessage, IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Trace, null, logMessage, new[] { envelope });

        public void LogTrace(ILogger logger, string logMessage, IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Trace, null, logMessage, envelopes);

        public void LogDebug(ILogger logger, string logMessage, IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Debug, null, logMessage, new[] { envelope });

        public void LogDebug(ILogger logger, string logMessage, IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Debug, null, logMessage, envelopes);

        public void LogInformation(ILogger logger, string logMessage, IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Information, null, logMessage, new[] { envelope });

        public void LogInformation(
            ILogger logger,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Information, null, logMessage, envelopes);

        public void LogWarning(ILogger logger, string logMessage, IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Warning, null, logMessage, new[] { envelope });

        public void LogWarning(ILogger logger, string logMessage, IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Warning, null, logMessage, envelopes);

        public void LogWarning(ILogger logger, Exception exception, string logMessage, IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Warning, exception, logMessage, new[] { envelope });

        public void LogWarning(
            ILogger logger,
            Exception exception,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Warning, exception, logMessage, envelopes);

        public void LogError(ILogger logger, string logMessage, IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Error, null, logMessage, new[] { envelope });

        public void LogError(ILogger logger, string logMessage, IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Error, null, logMessage, envelopes);

        public void LogError(ILogger logger, Exception exception, string logMessage, IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Error, exception, logMessage, new[] { envelope });

        public void LogError(
            ILogger logger,
            Exception exception,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Error, exception, logMessage, envelopes);

        public void LogCritical(ILogger logger, string logMessage, IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Critical, null, logMessage, new[] { envelope });

        public void LogCritical(ILogger logger, string logMessage, IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Critical, null, logMessage, envelopes);

        public void LogCritical(ILogger logger, Exception exception, string logMessage, IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Critical, exception, logMessage, new[] { envelope });

        public void LogCritical(
            ILogger logger,
            Exception exception,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Critical, exception, logMessage, envelopes);

        private void Log(
            ILogger logger,
            LogLevel logLevel,
            Exception exception,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes)
        {
            var properties = new List<(string, string, string)>();

            var firstMessage = envelopes.First();

            properties.Add(("Endpoint", "endpointName", firstMessage.Endpoint?.Name));

            var failedAttempts = firstMessage.Headers.GetValue<int>(DefaultMessageHeaders.FailedAttempts);
            if (failedAttempts > 0)
                properties.Add(("FailedAttempts", "failedAttempts", failedAttempts.ToString()));

            if (envelopes.Count == 1)
            {
                properties.Add(("Type", "messageType", firstMessage.Headers.GetValue(DefaultMessageHeaders.MessageType)));
                properties.Add(("Id", "messageId", firstMessage.Headers.GetValue(DefaultMessageHeaders.MessageId)));

                if (firstMessage.Offset != null)
                    properties.Add(("Offset", "offset", $"{firstMessage.Offset.Value}"));
            }

            properties.Add(("BatchId", "batchId", firstMessage.Headers.GetValue(DefaultMessageHeaders.BatchId)));
            properties.Add(("BatchSize", "batchSize", firstMessage.Headers.GetValue(DefaultMessageHeaders.BatchSize)));

            // Don't log empty values
            properties.RemoveAll(x => string.IsNullOrWhiteSpace(x.Item3));

            logger.Log(
                logLevel, exception,
                logMessage + " (" + string.Join(", ", properties.Select(p => $"{p.Item1}: {{{p.Item2}}}")) + ")",
                properties.Select(p => p.Item3).Cast<object>().ToArray());
        }

        #endregion

        #region Specific

        public void LogProcessing(ILogger logger, IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            LogInformation(logger,
                envelopes.Count > 1
                    ? $"Processing the batch of {envelopes.Count} inbound messages."
                    : "Processing inbound message.",
                envelopes);

        public void LogProcessingError(
            ILogger logger,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes,
            Exception exception) =>
            LogWarning(logger,
                exception,
                envelopes.Count > 1
                    ? $"Error occurred the batch of ({envelopes.Count} inbound messages."
                    : "Error occurred processing the inbound message.",
                envelopes);

        #endregion
    }
}