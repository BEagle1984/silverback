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

        public void LogTrace(ILogger logger, string logMessage, object message, IEndpoint endpoint = null, IOffset offset = null, MessageBatch batch = null) =>
            Log(logger, LogLevel.Trace, null, logMessage, message, endpoint, offset, null, batch);

        public void LogTrace(ILogger logger, string logMessage, IInboundMessage message, MessageBatch batch = null) =>
            Log(logger, LogLevel.Trace, null, logMessage, message, batch);

        public void LogInformation(ILogger logger, string logMessage, object message, IEndpoint endpoint = null, IOffset offset = null, MessageBatch batch = null) =>
            Log(logger, LogLevel.Information, null, logMessage, message, endpoint, offset, null, batch);

        public void LogInformation(ILogger logger, string logMessage, IInboundMessage message, MessageBatch batch = null) =>
            Log(logger, LogLevel.Information, null, logMessage, message, batch);

        public void LogWarning(ILogger logger, Exception exception, string logMessage, object message, IEndpoint endpoint = null, IOffset offset = null, MessageBatch batch = null) =>
            Log(logger, LogLevel.Warning, exception, logMessage, message, endpoint, offset, null, batch);

        public void LogWarning(ILogger logger, string logMessage, IInboundMessage message, MessageBatch batch = null) =>
            Log(logger, LogLevel.Warning, null, logMessage, message, batch);

        public void LogError(ILogger logger, Exception exception, string logMessage, object message, IEndpoint endpoint = null, IOffset offset = null, MessageBatch batch = null) =>
            Log(logger, LogLevel.Error, exception, logMessage, message, endpoint, offset, null, batch);

        public void LogError(ILogger logger, string logMessage, IInboundMessage message, MessageBatch batch = null) =>
            Log(logger, LogLevel.Error, null, logMessage, message, batch);

        public void LogCritical(ILogger logger, Exception exception, string logMessage, object message, IEndpoint endpoint = null, IOffset offset = null, MessageBatch batch = null) =>
            Log(logger, LogLevel.Critical, exception, logMessage, message, endpoint, offset, null, batch);

        public void LogCritical(ILogger logger, string logMessage, IInboundMessage message, MessageBatch batch = null) =>
            Log(logger, LogLevel.Critical, null, logMessage, message, batch);
        
        private void Log(ILogger logger, LogLevel logLevel, Exception exception, string logMessage, IInboundMessage message, MessageBatch batch) =>
            Log(logger, logLevel, exception, logMessage, message.Message, message.Endpoint, message.Offset, message.FailedAttempts, batch);

        private void Log(ILogger logger, LogLevel logLevel, Exception exception, string logMessage, object message,
            IEndpoint endpoint, IOffset offset, int? failedAttempts, MessageBatch batch)
        {
            var properties = new List<(string, string, object)>();
            
            if (offset != null)
                properties.Add(("offset", "offset", $"{offset.Key}@{offset.Value}"));
            
            if (endpoint != null)
                properties.Add(("endpoint", "endpointName", endpoint.Name));

            if (message != null && !(message is byte[]))
            {
                properties.Add(("type", "messageType", message.GetType().Name));

                var key = _messageKeyProvider.GetKey(message, false);
                if (key != null)
                    properties.Add(("id", "messageId", key));
            }

            if (failedAttempts != null && failedAttempts > 0)
                properties.Add(("failedAttempts", "failedAttempts", failedAttempts));

            if (batch != null)
            {
                properties.Add(("batchId", "batchId", batch.CurrentBatchId));
                properties.Add(("batchSize", "batchSize", batch.CurrentSize));
            }
            else if (message is IInboundBatch inboundBatch)
            {
                properties.Add(("batchId", "batchId", inboundBatch.Id));
                properties.Add(("batchSize", "batchSize", inboundBatch.Size));
            }

            logger.Log(
                logLevel, exception,
                logMessage + " {{" + string.Join(", ", properties.Select(p => $"{p.Item1}={{{p.Item2}}}")) + "}}",
                properties.Select(p => p.Item3).ToArray());
        }
    }
}
