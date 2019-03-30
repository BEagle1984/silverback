// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Batch;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    // TODO: Review and test
    public class MessageLogger
    {
        private readonly MessageKeyProvider _messageKeyProvider;

        public MessageLogger(MessageKeyProvider messageKeyProvider)
        {
            _messageKeyProvider = messageKeyProvider;
        }

        public void LogTrace(ILogger logger, string logMessage, object message, IEndpoint endpoint = null, MessageBatch batch = null, IOffset offset = null) =>
            Log(logger, LogLevel.Trace, null, logMessage, message, endpoint, batch, offset);

        public void LogWarning(ILogger logger, Exception exception, string logMessage, object message, IEndpoint endpoint = null, MessageBatch batch = null, IOffset offset = null) =>
            Log(logger, LogLevel.Warning, exception, logMessage, message, endpoint, batch, offset);

        public void LogError(ILogger logger, Exception exception, string logMessage, object message, IEndpoint endpoint = null, MessageBatch batch = null, IOffset offset = null) =>
            Log(logger, LogLevel.Error, exception, logMessage, message, endpoint, batch, offset);

        public void LogCritical(ILogger logger, Exception exception, string logMessage, object message, IEndpoint endpoint = null, MessageBatch batch = null, IOffset offset = null) =>
            Log(logger, LogLevel.Critical, exception, logMessage, message, endpoint, batch, offset);

        public void Log(ILogger logger, LogLevel logLevel, Exception exception, string logMessage, object message, IEndpoint endpoint = null, MessageBatch batch = null, IOffset offset = null)
        {
            var failedMessage = message as FailedMessage;

            var innerMessage = failedMessage?.Message ?? message;

            var properties = new List<(string, string, object)>();

            var key = _messageKeyProvider.GetKey(message, false);
            if (key != null)
                properties.Add(("id", "messageId", key));

            if (offset != null)
                properties.Add(("offset", "offset", $"{offset.Key}@{offset.Value}"));

            if (endpoint != null)
                properties.Add(("endpoint", "endpointName", endpoint.Name));

            properties.Add(("type", "messageType", innerMessage.GetType().Name));

            if (batch != null)
            {
                properties.Add(("batchId", "batchId", batch.CurrentBatchId));
                properties.Add(("batchSize", "batchSize", batch.CurrentSize));
            }
            else if (message is BatchEvent batchMessage)
            {
                properties.Add(("batchId", "batchId", batchMessage.BatchId));
                properties.Add(("batchSize", "batchSize", batchMessage.BatchSize));
            }

            if (failedMessage != null)
                properties.Add(("failedAttempts", "failedAttempts", failedMessage.FailedAttempts));

                logger.Log(
                        logLevel, exception,
                        logMessage + " {{" + string.Join(", ", properties.Select(p => $"{p.Item1}={{{p.Item2}}}")) + "}}",
                        properties.Select(p => p.Item3).ToArray());
        }
    }
}
