// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    // TODO: Test
    public class MessageLogger
    {
        private readonly MessageKeyProvider _messageKeyProvider;

        public MessageLogger(MessageKeyProvider messageKeyProvider)
        {
            _messageKeyProvider = messageKeyProvider;
        }

        #region Generic

        public void LogTrace(ILogger logger, string logMessage, IBrokerMessage message) =>
            Log(logger, LogLevel.Trace, null, logMessage, new[]{ message });
        public void LogTrace(ILogger logger, string logMessage, IEnumerable<IBrokerMessage> messages) =>
            Log(logger, LogLevel.Trace, null, logMessage, messages);

        public void LogInformation(ILogger logger, string logMessage, IBrokerMessage message) =>
            Log(logger, LogLevel.Information, null, logMessage, new[] { message });
        public void LogInformation(ILogger logger, string logMessage, IEnumerable<IBrokerMessage> messages) =>
            Log(logger, LogLevel.Information, null, logMessage, messages);

        public void LogWarning(ILogger logger, string logMessage, IBrokerMessage message) =>
            Log(logger, LogLevel.Warning, null, logMessage, new[] { message });
        public void LogWarning(ILogger logger, string logMessage, IEnumerable<IBrokerMessage> messages) =>
            Log(logger, LogLevel.Warning, null, logMessage, messages);
        public void LogWarning(ILogger logger, Exception exception, string logMessage, IBrokerMessage message) =>
            Log(logger, LogLevel.Warning, exception, logMessage, new[] { message });
        public void LogWarning(ILogger logger, Exception exception, string logMessage, IEnumerable<IBrokerMessage> messages) =>
            Log(logger, LogLevel.Warning, exception, logMessage, messages);

        public void LogError(ILogger logger, string logMessage, IBrokerMessage message) =>
            Log(logger, LogLevel.Error, null, logMessage, new[] { message });
        public void LogError(ILogger logger, string logMessage, IEnumerable<IBrokerMessage> messages) =>
            Log(logger, LogLevel.Error, null, logMessage, messages);
        public void LogError(ILogger logger, Exception exception, string logMessage, IBrokerMessage message) =>
            Log(logger, LogLevel.Error, exception, logMessage, new[] { message });
        public void LogError(ILogger logger, Exception exception, string logMessage, IEnumerable<IBrokerMessage> messages) =>
            Log(logger, LogLevel.Error, exception, logMessage, messages);

        public void LogCritical(ILogger logger, string logMessage, IBrokerMessage message) =>
            Log(logger, LogLevel.Critical, null, logMessage, new[] { message });
        public void LogCritical(ILogger logger, string logMessage, IEnumerable<IBrokerMessage> messages) =>
            Log(logger, LogLevel.Critical, null, logMessage, messages);
        public void LogCritical(ILogger logger, Exception exception, string logMessage, IInboundMessage message) =>
            Log(logger, LogLevel.Critical, exception, logMessage, new[] { message });
        public void LogCritical(ILogger logger, Exception exception, string logMessage, IEnumerable<IInboundMessage> messages) =>
            Log(logger, LogLevel.Critical, exception, logMessage, messages);

        private void Log(ILogger logger, LogLevel logLevel, Exception exception, string logMessage, IEnumerable<IBrokerMessage> messages)
        {
            var properties = new List<(string, string, string)>();

            var firstMessage = messages.First();

            properties.Add(("endpoint", "endpointName", firstMessage.Endpoint.Name));
            properties.Add(("failedAttempts", "failedAttempts", firstMessage.Headers.GetValue<string>(MessageHeader.FailedAttemptsKey)));

            if (messages.Count() == 1)
            {
                properties.Add(("offset", "offset", $"{firstMessage.Offset.Key}@{firstMessage.Offset.Value}"));
                properties.Add(("type", "messageType", firstMessage.Headers.GetValue<string>(MessageHeader.MessageTypeKey)));
                properties.Add(("id", "messageId", firstMessage.Headers.GetValue<string>(MessageHeader.MessageIdKey)));
            }

            properties.Add(("batchId", "batchId", firstMessage.Headers.GetValue<string>(MessageHeader.BatchIdKey)));
            properties.Add(("batchSize", "batchSize", firstMessage.Headers.GetValue<string>(MessageHeader.BatchSizeKey)));

            // Don't log empty values
            properties.RemoveAll(x => string.IsNullOrWhiteSpace(x.Item3));

            logger.Log(
                logLevel, exception,
                logMessage + " {{" + string.Join(", ", properties.Select(p => $"{p.Item1}={{{p.Item2}}}")) + "}}",
                properties.Select(p => p.Item3).Cast<object>().ToArray());
        }

        #endregion

        #region Specific

        public void LogProcessing(ILogger logger, IEnumerable<IBrokerMessage> messages) =>
            LogInformation(logger,
                messages.Count() > 1
                    ? $"Processing the batch of {messages.Count()} inbound messages."
                    : "Processing inbound message.",
                messages);

        public void LogProcessingError(ILogger logger, IEnumerable<IBrokerMessage> messages, Exception exception) =>
            LogWarning(logger,
                exception,
                messages.Count() > 1
                    ? $"Error occurred the batch of ({messages.Count()} inbound messages."
                    : "Error occurred processing the inbound message.",
                messages);

        #endregion
    }
}
