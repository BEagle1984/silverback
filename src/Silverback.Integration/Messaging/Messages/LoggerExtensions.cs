// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Adds some methods to the <see cref="ILogger" /> used to consistently enrich the log entry with the
    ///     information about the message(s) being consumed.
    /// </summary>
    // TODO: Test
    public static class LoggerExtensions
    {
        /// <summary>
        ///     Writes the standard <i> "Processing inbound message" </i> or
        ///     <i> "Processing the batch of # inbound messages" </i> log message.
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="envelopes">
        ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the messages being processed.
        /// </param>
        public static void LogProcessing(this ILogger logger, IReadOnlyCollection<IRawBrokerEnvelope> envelopes)
        {
            Check.NotEmpty(envelopes, nameof(envelopes));

            var message = envelopes.Count > 1
                ? $"Processing the batch of {envelopes.Count} inbound messages."
                : "Processing inbound message.";

            LogInformation(logger, EventIds.ProcessingInboundMessage, message, envelopes);
        }

        /// <summary>
        ///     Writes the standard <i> "Error occurred processing the inbound message" </i> or
        ///     <i> "Error occurred processing the batch of # inbound messages" </i> log message.
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="envelopes">
        ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the messages being processed.
        /// </param>
        /// <param name="exception"> The exception to log. </param>
        public static void LogProcessingError(
            this ILogger logger,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes,
            Exception exception)
        {
            Check.NotEmpty(envelopes, nameof(envelopes));

            var message = envelopes.Count > 1
                ? $"Error occurred processing the batch of {envelopes.Count} inbound messages."
                : "Error occurred processing the inbound message.";

            LogWarning(logger, EventIds.ProcessingInboundMessageError, exception, message, envelopes);
        }

        /// <summary>
        ///     Writes a trace log message, enriching it with the information related to the provided message.
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelope">
        ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        /// </param>
        public static void LogTrace(
            this ILogger logger,
            EventId eventId,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Trace, eventId, null, logMessage, new[] { envelope });

        /// <summary>
        ///     Writes a trace log message, enriching it with the information related to the provided message(s).
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelopes">
        ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        ///     log.
        /// </param>
        public static void LogTrace(
            this ILogger logger,
            EventId eventId,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Trace, eventId, null, logMessage, envelopes);

        /// <summary>
        ///     Writes a debug log message, enriching it with the information related to the provided message.
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelope">
        ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        /// </param>
        public static void LogDebug(
            this ILogger logger,
            EventId eventId,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Debug, eventId, null, logMessage, new[] { envelope });

        /// <summary>
        ///     Writes a debug log message, enriching it with the information related to the provided message(s).
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelopes">
        ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        ///     log.
        /// </param>
        public static void LogDebug(
            this ILogger logger,
            EventId eventId,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Debug, eventId, null, logMessage, envelopes);

        /// <summary>
        ///     Writes an information log message, enriching it with the information related to the provided
        ///     message.
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelope">
        ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        /// </param>
        public static void LogInformation(
            this ILogger logger,
            EventId eventId,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Information, eventId, null, logMessage, new[] { envelope });

        /// <summary>
        ///     Writes an information log message, enriching it with the information related to the provided
        ///     message(s).
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelopes">
        ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        ///     log.
        /// </param>
        public static void LogInformation(
            this ILogger logger,
            EventId eventId,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Information, eventId, null, logMessage, envelopes);

        /// <summary>
        ///     Writes a warning log message, enriching it with the information related to the provided message.
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelope">
        ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        /// </param>
        public static void LogWarning(
            this ILogger logger,
            EventId eventId,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Warning, eventId, null, logMessage, new[] { envelope });

        /// <summary>
        ///     Writes a warning log message, enriching it with the information related to the provided message(s).
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelopes">
        ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        ///     log.
        /// </param>
        public static void LogWarning(
            this ILogger logger,
            EventId eventId,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Warning, eventId, null, logMessage, envelopes);

        /// <summary>
        ///     Writes a warning log message, enriching it with the information related to the provided message.
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="exception"> The exception to log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelope">
        ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        /// </param>
        public static void LogWarning(
            this ILogger logger,
            EventId eventId,
            Exception exception,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Warning, eventId, exception, logMessage, new[] { envelope });

        /// <summary>
        ///     Writes a warning log message, enriching it with the information related to the provided message(s).
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="exception"> The exception to log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelopes">
        ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        ///     log.
        /// </param>
        public static void LogWarning(
            this ILogger logger,
            EventId eventId,
            Exception exception,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Warning, eventId, exception, logMessage, envelopes);

        /// <summary>
        ///     Writes an error log message, enriching it with the information related to the provided message.
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelope">
        ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        /// </param>
        public static void LogError(
            this ILogger logger,
            EventId eventId,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Error, eventId, null, logMessage, new[] { envelope });

        /// <summary>
        ///     Writes an error log message, enriching it with the information related to the provided message(s).
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelopes">
        ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        ///     log.
        /// </param>
        public static void LogError(
            this ILogger logger,
            EventId eventId,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Error, eventId, null, logMessage, envelopes);

        /// <summary>
        ///     Writes an error log message, enriching it with the information related to the provided message.
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="exception"> The exception to log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelope">
        ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        /// </param>
        public static void LogError(
            this ILogger logger,
            EventId eventId,
            Exception exception,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Error, eventId, exception, logMessage, new[] { envelope });

        /// <summary>
        ///     Writes an error log message, enriching it with the information related to the provided message(s).
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="exception"> The exception to log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelopes">
        ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        ///     log.
        /// </param>
        public static void LogError(
            this ILogger logger,
            EventId eventId,
            Exception exception,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Error, eventId, exception, logMessage, envelopes);

        /// <summary>
        ///     Writes a critical log message, enriching it with the information related to the provided message.
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelope">
        ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        /// </param>
        public static void LogCritical(
            ILogger logger,
            EventId eventId,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Critical, eventId, null, logMessage, new[] { envelope });

        /// <summary>
        ///     Writes a critical log message, enriching it with the information related to the provided message(s).
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelopes">
        ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        ///     log.
        /// </param>
        public static void LogCritical(
            this ILogger logger,
            EventId eventId,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Critical, eventId, null, logMessage, envelopes);

        /// <summary>
        ///     Writes a critical log message, enriching it with the information related to the provided message.
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="exception"> The exception to log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelope">
        ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        /// </param>
        public static void LogCritical(
            this ILogger logger,
            EventId eventId,
            Exception exception,
            string logMessage,
            IRawBrokerEnvelope envelope) =>
            Log(logger, LogLevel.Critical, eventId, exception, logMessage, new[] { envelope });

        /// <summary>
        ///     Writes a critical log message, enriching it with the information related to the provided message(s).
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="exception"> The exception to log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelopes">
        ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        ///     log.
        /// </param>
        public static void LogCritical(
            this ILogger logger,
            EventId eventId,
            Exception exception,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes) =>
            Log(logger, LogLevel.Critical, eventId, exception, logMessage, envelopes);

        /// <summary>
        ///     Writes a log message at the specified log level, enriching it with the information related to the
        ///     provided message(s).
        /// </summary>
        /// <param name="logger"> The <see cref="ILogger" /> to write to. </param>
        /// <param name="logLevel"> Entry will be written on this level. </param>
        /// <param name="eventId"> The event id associated with the log. </param>
        /// <param name="exception"> The exception to log. </param>
        /// <param name="logMessage"> The log message. </param>
        /// <param name="envelopes">
        ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        ///     log.
        /// </param>
        public static void Log(
            this ILogger logger,
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            string logMessage,
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes)
        {
            Check.NotEmpty(logMessage, nameof(logMessage));
            Check.NotEmpty(envelopes, nameof(envelopes));

            IList<(string, string, string)> arguments = GetLogArguments(envelopes);

            string argumentNames = string.Join(", ", arguments.Select(p => $"{p.Item1}: {{{p.Item2}}}"));
            string enrichedLogMessage = $"{logMessage} ({argumentNames})";
            object[] argumentValues = arguments.Select(p => p.Item3).Cast<object>().ToArray();

            if (exception != null)
                logger.Log(logLevel, eventId, exception, enrichedLogMessage, argumentValues);
            else
                logger.Log(logLevel, eventId, enrichedLogMessage, argumentValues);
        }

        private static IList<(string, string, string)> GetLogArguments(
            IReadOnlyCollection<IRawBrokerEnvelope> envelopes)
        {
            var args = new List<(string, string, string?)>();

            var firstMessage = envelopes.First();

            if (firstMessage is IRawInboundEnvelope inboundEnvelope &&
                !string.IsNullOrEmpty(inboundEnvelope.ActualEndpointName))
            {
                args.Add(("Endpoint", "endpointName", inboundEnvelope.ActualEndpointName));
            }
            else
            {
                args.Add(("Endpoint", "endpointName", firstMessage.Endpoint?.Name));
            }

            var failedAttempts = firstMessage.Headers.GetValue<int>(DefaultMessageHeaders.FailedAttempts);
            if (failedAttempts > 0)
                args.Add(("FailedAttempts", "failedAttempts", failedAttempts.ToString()));

            if (envelopes.Count == 1)
            {
                args.Add(("Type", "messageType", firstMessage.Headers.GetValue(DefaultMessageHeaders.MessageType)));
                args.Add(("Id", "messageId", firstMessage.Headers.GetValue(DefaultMessageHeaders.MessageId)));

                if (firstMessage.Offset != null)
                    args.Add(("Offset", "offset", $"{firstMessage.Offset.ToLogString()}"));
            }

            args.Add(("BatchId", "batchId", firstMessage.Headers.GetValue(DefaultMessageHeaders.BatchId)));
            args.Add(("BatchSize", "batchSize", firstMessage.Headers.GetValue(DefaultMessageHeaders.BatchSize)));

            // Don't log empty values
            args.RemoveAll(x => string.IsNullOrWhiteSpace(x.Item3));

            return args!;
        }
    }
}
