﻿// Copyright (c) 2020 Sergio Aquilini
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

        public void LogTrace(ILogger logger, string logMessage, IRawBrokerMessage message) =>
            Log(logger, LogLevel.Trace, null, logMessage, new[] { message });

        public void LogTrace(ILogger logger, string logMessage, IEnumerable<IRawBrokerMessage> messages) =>
            Log(logger, LogLevel.Trace, null, logMessage, messages);

        public void LogDebug(ILogger logger, string logMessage, IRawBrokerMessage message) =>
            Log(logger, LogLevel.Debug, null, logMessage, new[] { message });

        public void LogDebug(ILogger logger, string logMessage, IEnumerable<IRawBrokerMessage> messages) =>
            Log(logger, LogLevel.Debug, null, logMessage, messages);

        public void LogInformation(ILogger logger, string logMessage, IRawBrokerMessage message) =>
            Log(logger, LogLevel.Information, null, logMessage, new[] { message });

        public void LogInformation(ILogger logger, string logMessage, IEnumerable<IRawBrokerMessage> messages) =>
            Log(logger, LogLevel.Information, null, logMessage, messages);

        public void LogWarning(ILogger logger, string logMessage, IRawBrokerMessage message) =>
            Log(logger, LogLevel.Warning, null, logMessage, new[] { message });

        public void LogWarning(ILogger logger, string logMessage, IEnumerable<IRawBrokerMessage> messages) =>
            Log(logger, LogLevel.Warning, null, logMessage, messages);

        public void LogWarning(ILogger logger, Exception exception, string logMessage, IRawBrokerMessage message) =>
            Log(logger, LogLevel.Warning, exception, logMessage, new[] { message });

        public void LogWarning(
            ILogger logger,
            Exception exception,
            string logMessage,
            IEnumerable<IRawBrokerMessage> messages) =>
            Log(logger, LogLevel.Warning, exception, logMessage, messages);

        public void LogError(ILogger logger, string logMessage, IRawBrokerMessage message) =>
            Log(logger, LogLevel.Error, null, logMessage, new[] { message });

        public void LogError(ILogger logger, string logMessage, IEnumerable<IRawBrokerMessage> messages) =>
            Log(logger, LogLevel.Error, null, logMessage, messages);

        public void LogError(ILogger logger, Exception exception, string logMessage, IRawBrokerMessage message) =>
            Log(logger, LogLevel.Error, exception, logMessage, new[] { message });

        public void LogError(
            ILogger logger,
            Exception exception,
            string logMessage,
            IEnumerable<IRawBrokerMessage> messages) =>
            Log(logger, LogLevel.Error, exception, logMessage, messages);

        public void LogCritical(ILogger logger, string logMessage, IRawBrokerMessage message) =>
            Log(logger, LogLevel.Critical, null, logMessage, new[] { message });

        public void LogCritical(ILogger logger, string logMessage, IEnumerable<IRawBrokerMessage> messages) =>
            Log(logger, LogLevel.Critical, null, logMessage, messages);

        public void LogCritical(ILogger logger, Exception exception, string logMessage, IRawBrokerMessage message) =>
            Log(logger, LogLevel.Critical, exception, logMessage, new[] { message });

        public void LogCritical(
            ILogger logger,
            Exception exception,
            string logMessage,
            IEnumerable<IRawBrokerMessage> messages) =>
            Log(logger, LogLevel.Critical, exception, logMessage, messages);

        private void Log(
            ILogger logger,
            LogLevel logLevel,
            Exception exception,
            string logMessage,
            IEnumerable<IRawBrokerMessage> messages)
        {
            var properties = new List<(string, string, string)>();

            var firstMessage = messages.First();

            properties.Add(("Endpoint", "endpointName", firstMessage.Endpoint?.Name));

            var failedAttempts = firstMessage.Headers.GetValue<int>(MessageHeader.FailedAttemptsKey);
            if (failedAttempts > 0)
                properties.Add(("FailedAttempts", "failedAttempts", failedAttempts.ToString()));

            if (messages.Count() == 1)
            {
                properties.Add(("Type", "messageType", firstMessage.Headers.GetValue(MessageHeader.MessageTypeKey)));
                properties.Add(("Id", "messageId", firstMessage.Headers.GetValue(MessageHeader.MessageIdKey)));

                if (firstMessage.Offset != null)
                    properties.Add(("Offset", "offset", $"{firstMessage.Offset.Value}"));
            }

            properties.Add(("BatchId", "batchId", firstMessage.Headers.GetValue(MessageHeader.BatchIdKey)));
            properties.Add(("BatchSize", "batchSize", firstMessage.Headers.GetValue(MessageHeader.BatchSizeKey)));

            // Don't log empty values
            properties.RemoveAll(x => string.IsNullOrWhiteSpace(x.Item3));

            logger.Log(
                logLevel, exception,
                logMessage + " (" + string.Join(", ", properties.Select(p => $"{p.Item1}: {{{p.Item2}}}")) + ")",
                properties.Select(p => p.Item3).Cast<object>().ToArray());
        }

        #endregion

        #region Specific

        public void LogProcessing(ILogger logger, IEnumerable<IRawBrokerMessage> messages) =>
            LogInformation(logger,
                messages.Count() > 1
                    ? $"Processing the batch of {messages.Count()} inbound messages."
                    : "Processing inbound message.",
                messages);

        public void LogProcessingError(ILogger logger, IEnumerable<IRawBrokerMessage> messages, Exception exception) =>
            LogWarning(logger,
                exception,
                messages.Count() > 1
                    ? $"Error occurred the batch of ({messages.Count()} inbound messages."
                    : "Error occurred processing the inbound message.",
                messages);

        #endregion
    }
}