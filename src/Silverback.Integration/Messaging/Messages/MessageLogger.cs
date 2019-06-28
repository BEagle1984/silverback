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

        public void LogTrace(ILogger logger, string logMessage, object message, IEndpoint endpoint = null, IOffset offset = null, Guid? batchId = null, int? batchSize = null) =>
            Log(logger, LogLevel.Trace, null, logMessage, message, endpoint, offset, null, batchId, batchSize);

        public void LogTrace(ILogger logger, string logMessage, IInboundMessage message, Guid? batchId = null, int? batchSize = null) =>
            Log(logger, LogLevel.Trace, null, logMessage, message, batchId, batchSize);

        public void LogInformation(ILogger logger, string logMessage, object message, IEndpoint endpoint = null, IOffset offset = null, Guid? batchId = null, int? batchSize = null) =>
            Log(logger, LogLevel.Information, null, logMessage, message, endpoint, offset, null, batchId, batchSize);

        public void LogInformation(ILogger logger, string logMessage, IInboundMessage message, Guid? batchId = null, int? batchSize = null) =>
            Log(logger, LogLevel.Information, null, logMessage, message, batchId, batchSize);

        public void LogWarning(ILogger logger, Exception exception, string logMessage, object message, IEndpoint endpoint = null, IOffset offset = null, Guid? batchId = null, int? batchSize = null) =>
            Log(logger, LogLevel.Warning, exception, logMessage, message, endpoint, offset, null, batchId, batchSize);

        public void LogWarning(ILogger logger, Exception exception, string logMessage, IInboundMessage message, Guid? batchId = null, int? batchSize = null) =>
            Log(logger, LogLevel.Warning, exception, logMessage, message, batchId, batchSize);

        public void LogError(ILogger logger, Exception exception, string logMessage, object message, IEndpoint endpoint = null, IOffset offset = null, Guid? batchId = null, int? batchSize = null) =>
            Log(logger, LogLevel.Error, exception, logMessage, message, endpoint, offset, null, batchId, batchSize);

        public void LogError(ILogger logger, string logMessage, IInboundMessage message, Guid? batchId = null, int? batchSize = null) =>
            Log(logger, LogLevel.Error, null, logMessage, message, batchId, batchSize);

        public void LogCritical(ILogger logger, Exception exception, string logMessage, object message, IEndpoint endpoint = null, IOffset offset = null, Guid? batchId = null, int? batchSize = null) =>
            Log(logger, LogLevel.Critical, exception, logMessage, message, endpoint, offset, null, batchId, batchSize);

        public void LogCritical(ILogger logger, string logMessage, IInboundMessage message, Guid? batchId = null, int? batchSize = null) =>
            Log(logger, LogLevel.Critical, null, logMessage, message, batchId, batchSize);
        
        private void Log(ILogger logger, LogLevel logLevel, Exception exception, string logMessage, IInboundMessage message, Guid? batchId, int? batchSize) =>
            Log(logger, logLevel, exception, logMessage, message.Message, message.Endpoint, message.Offset, message.FailedAttempts, batchId, batchSize);

        private void Log(ILogger logger, LogLevel logLevel, Exception exception, string logMessage, object message,
            IEndpoint endpoint, IOffset offset, int? failedAttempts, Guid? batchId, int? batchSize)
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

            if (batchId != null)
                properties.Add(("batchId", "batchId", batchId));

            if (batchSize != null)
                properties.Add(("batchSize", "batchSize", batchSize));

            logger.Log(
                logLevel, exception,
                logMessage + " {{" + string.Join(", ", properties.Select(p => $"{p.Item1}={{{p.Item2}}}")) + "}}",
                properties.Select(p => p.Item3).ToArray());
        }

        #endregion

        #region Specific

        public void LogProcessing(ILogger logger, IEnumerable<IInboundMessage> messages)
        {
            if ()
            LogInformation(logger,
                batch != null
                    ? "Processing inbound batch."
                    : "Processing inbound message.",
                message,
                batch?.Id,
                batch?.Size);
        }

        public void LogProcessingError(ILogger logger, IInboundMessage message, Exception exception)
        {
            var batch = message as IInboundBatch;

            LogWarning(logger,
                exception,
                batch != null
                    ? "Error occurred processing the inbound batch."
                    : "Error occurred processing the inbound message.",
                message,
                batch?.Id,
                batch?.Size);
        }

        #endregion
    }
}
