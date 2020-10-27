// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     Extends the <see cref="ISilverbackLogger{TCategoryName}" /> adding some methods used to consistently
    ///     enrich the log entry with the information about the message(s) being produced or consumed.
    /// </summary>
    public interface ISilverbackIntegrationLogger : ISilverbackLogger
    {
        /// <summary>
        ///     Writes the standard <i>"Processing inbound message"</i> or
        ///     <i>"Processing the batch of # inbound messages"</i> log message.
        /// </summary>
        /// <param name="context">
        ///     The <see cref="ConsumerPipelineContext" /> related to the message being processed.
        /// </param>
        void LogProcessing(ConsumerPipelineContext context);

        /// <summary>
        ///     Writes the standard <i>"Error occurred processing the inbound message"</i> or
        ///     <i>"Error occurred processing the batch of # inbound messages"</i> log message.
        /// </summary>
        /// <param name="context">
        ///     The <see cref="ConsumerPipelineContext" /> related to the message being processed.
        /// </param>
        /// <param name="exception">
        ///     The exception to log.
        /// </param>
        void LogProcessingError(ConsumerPipelineContext context, Exception exception);

        /// <summary>
        ///     Writes the standard <i>"Error occurred processing the inbound sequence of messages"</i> or
        ///     <i>"Sequence processing has been aborted"</i> log message.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="IRawInboundEnvelope" /> containing the message being processed.
        /// </param>
        /// <param name="sequence">
        ///     The <see cref="ISequence" /> being aborted.
        /// </param>
        /// <param name="reason">
        ///     The abort reason.
        /// </param>
        /// <param name="exception">
        ///     The exception that caused the abort, if an exception was thrown.
        /// </param>
        void LogSequenceAborted(
            IRawInboundEnvelope envelope,
            ISequence sequence,
            SequenceAbortReason reason,
            Exception? exception);

        /// <summary>
        ///     Writes a trace log message, enriching it with the information related to the provided message.
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="context">
        ///     The <see cref="ConsumerPipelineContext" /> related to the message being processed.
        /// </param>
        void LogTraceWithMessageInfo(
            EventId eventId,
            string logMessage,
            ConsumerPipelineContext context);

        /// <summary>
        ///     Writes a trace log message, enriching it with the information related to the provided message(s).
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="envelope">
        ///     The <see cref="IRawOutboundEnvelope" /> containing the message related to the this log.
        /// </param>
        void LogTraceWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawOutboundEnvelope envelope);

        /// <summary>
        ///     Writes a debug log message, enriching it with the information related to the provided message.
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="context">
        ///     The <see cref="ConsumerPipelineContext" /> related to the message being processed.
        /// </param>
        void LogDebugWithMessageInfo(
            EventId eventId,
            string logMessage,
            ConsumerPipelineContext context);

        /// <summary>
        ///     Writes a debug log message, enriching it with the information related to the provided message(s).
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="envelope">
        ///     The <see cref="IRawOutboundEnvelope" /> containing the message related to the this log.
        /// </param>
        void LogDebugWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawOutboundEnvelope envelope);

        /// <summary>
        ///     Writes an information log message, enriching it with the information related to the provided
        ///     message.
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="context">
        ///     The <see cref="ConsumerPipelineContext" /> related to the message being processed.
        /// </param>
        void LogInformationWithMessageInfo(
            EventId eventId,
            string logMessage,
            ConsumerPipelineContext context);

        /// <summary>
        ///     Writes an information log message, enriching it with the information related to the provided
        ///     message(s).
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="envelope">
        ///     The <see cref="IRawOutboundEnvelope" /> containing the message related to the this log.
        /// </param>
        void LogInformationWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawOutboundEnvelope envelope);

        /// <summary>
        ///     Writes a warning log message, enriching it with the information related to the provided message.
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="context">
        ///     The <see cref="ConsumerPipelineContext" /> related to the message being processed.
        /// </param>
        void LogWarningWithMessageInfo(
            EventId eventId,
            string logMessage,
            ConsumerPipelineContext context);

        /// <summary>
        ///     Writes a warning log message, enriching it with the information related to the provided message(s).
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="envelope">
        ///     The <see cref="IRawOutboundEnvelope" /> containing the message related to the this log.
        /// </param>
        void LogWarningWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawOutboundEnvelope envelope);

        /// <summary>
        ///     Writes a warning log message, enriching it with the information related to the provided message.
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="exception">
        ///     The exception to log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="context">
        ///     The <see cref="ConsumerPipelineContext" /> related to the message being processed.
        /// </param>
        void LogWarningWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            ConsumerPipelineContext context);

        /// <summary>
        ///     Writes a warning log message, enriching it with the information related to the provided message(s).
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="exception">
        ///     The exception to log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="envelope">
        ///     The <see cref="IRawOutboundEnvelope" /> containing the message related to the this log.
        /// </param>
        void LogWarningWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            IRawOutboundEnvelope envelope);

        /// <summary>
        ///     Writes an error log message, enriching it with the information related to the provided message.
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="context">
        ///     The <see cref="ConsumerPipelineContext" /> related to the message being processed.
        /// </param>
        void LogErrorWithMessageInfo(
            EventId eventId,
            string logMessage,
            ConsumerPipelineContext context);

        /// <summary>
        ///     Writes an error log message, enriching it with the information related to the provided message(s).
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="envelope">
        ///     The <see cref="IRawOutboundEnvelope" /> containing the message related to the this log.
        /// </param>
        void LogErrorWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawOutboundEnvelope envelope);

        /// <summary>
        ///     Writes an error log message, enriching it with the information related to the provided message.
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="exception">
        ///     The exception to log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="context">
        ///     The <see cref="ConsumerPipelineContext" /> related to the message being processed.
        /// </param>
        void LogErrorWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            ConsumerPipelineContext context);

        /// <summary>
        ///     Writes an error log message, enriching it with the information related to the provided message(s).
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="exception">
        ///     The exception to log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="envelope">
        ///     The <see cref="IRawOutboundEnvelope" /> containing the message related to the this log.
        /// </param>
        void LogErrorWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            IRawOutboundEnvelope envelope);

        /// <summary>
        ///     Writes a critical log message, enriching it with the information related to the provided message.
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="context">
        ///     The <see cref="ConsumerPipelineContext" /> related to the message being processed.
        /// </param>
        void LogCriticalWithMessageInfo(
            EventId eventId,
            string logMessage,
            ConsumerPipelineContext context);

        /// <summary>
        ///     Writes an critical log message, enriching it with the information related to the provided message(s).
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="envelope">
        ///     The <see cref="IRawOutboundEnvelope" /> containing the message related to the this log.
        /// </param>
        void LogCriticalWithMessageInfo(
            EventId eventId,
            string logMessage,
            IRawOutboundEnvelope envelope);

        /// <summary>
        ///     Writes an critical log message, enriching it with the information related to the provided message.
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="exception">
        ///     The exception to log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="context">
        ///     The <see cref="ConsumerPipelineContext" /> related to the message being processed.
        /// </param>
        void LogCriticalWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            ConsumerPipelineContext context);

        /// <summary>
        ///     Writes a critical log message, enriching it with the information related to the provided message(s).
        /// </summary>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="exception">
        ///     The exception to log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="envelope">
        ///     The <see cref="IRawOutboundEnvelope" /> containing the message related to the this log.
        /// </param>
        void LogCriticalWithMessageInfo(
            EventId eventId,
            Exception exception,
            string logMessage,
            IRawOutboundEnvelope envelope);

        /// <summary>
        ///     Writes a log message at the specified log level, enriching it with the information related to the
        ///     provided message(s).
        /// </summary>
        /// <param name="logLevel">
        ///     Entry will be written on this level.
        /// </param>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="exception">
        ///     The exception to log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="context">
        ///     The <see cref="ConsumerPipelineContext" /> related to the message being processed.
        /// </param>
        void LogWithMessageInfo(
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            string logMessage,
            ConsumerPipelineContext context);

        /// <summary>
        ///     Writes a log message at the specified log level, enriching it with the information related to the
        ///     provided message(s).
        /// </summary>
        /// <param name="logLevel">
        ///     Entry will be written on this level.
        /// </param>
        /// <param name="eventId">
        ///     The event id associated with the log.
        /// </param>
        /// <param name="exception">
        ///     The exception to log.
        /// </param>
        /// <param name="logMessage">
        ///     The log message.
        /// </param>
        /// <param name="envelope">
        ///     The <see cref="IRawOutboundEnvelope" /> containing the message related to the this log.
        /// </param>
        void LogWithMessageInfo(
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            string logMessage,
            IRawOutboundEnvelope envelope);

        // /// <summary>
        // ///     Writes a trace log message, enriching it with the information related to the provided message.
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelope">
        // ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        // /// </param>
        // void LogTraceWithMessageInfo(
        //     EventId eventId,
        //     string logMessage,
        //     IRawBrokerEnvelope envelope);
        //
        // /// <summary>
        // ///     Writes a trace log message, enriching it with the information related to the provided message(s).
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelopes">
        // ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        // ///     log.
        // /// </param>
        // void LogTraceWithMessageInfo(
        //     EventId eventId,
        //     string logMessage,
        //     IReadOnlyCollection<IRawBrokerEnvelope> envelopes);
        //
        // /// <summary>
        // ///     Writes a debug log message, enriching it with the information related to the provided message.
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelope">
        // ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        // /// </param>
        // void LogDebugWithMessageInfo(
        //     EventId eventId,
        //     string logMessage,
        //     IRawBrokerEnvelope envelope);
        //
        // /// <summary>
        // ///     Writes a debug log message, enriching it with the information related to the provided message(s).
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelopes">
        // ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        // ///     log.
        // /// </param>
        // void LogDebugWithMessageInfo(
        //     EventId eventId,
        //     string logMessage,
        //     IReadOnlyCollection<IRawBrokerEnvelope> envelopes);
        //
        // /// <summary>
        // ///     Writes an information log message, enriching it with the information related to the provided
        // ///     message.
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelope">
        // ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        // /// </param>
        // void LogInformationWithMessageInfo(
        //     EventId eventId,
        //     string logMessage,
        //     IRawBrokerEnvelope envelope);
        //
        // /// <summary>
        // ///     Writes an information log message, enriching it with the information related to the provided
        // ///     message(s).
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelopes">
        // ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        // ///     log.
        // /// </param>
        // void LogInformationWithMessageInfo(
        //     EventId eventId,
        //     string logMessage,
        //     IReadOnlyCollection<IRawBrokerEnvelope> envelopes);
        //
        // /// <summary>
        // ///     Writes a warning log message, enriching it with the information related to the provided message.
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelope">
        // ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        // /// </param>
        // void LogWarningWithMessageInfo(
        //     EventId eventId,
        //     string logMessage,
        //     IRawBrokerEnvelope envelope);
        //
        // /// <summary>
        // ///     Writes a warning log message, enriching it with the information related to the provided message(s).
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelopes">
        // ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        // ///     log.
        // /// </param>
        // void LogWarningWithMessageInfo(
        //     EventId eventId,
        //     string logMessage,
        //     IReadOnlyCollection<IRawBrokerEnvelope> envelopes);
        //
        // /// <summary>
        // ///     Writes a warning log message, enriching it with the information related to the provided message.
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="exception">
        // ///     The exception to log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelope">
        // ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        // /// </param>
        // void LogWarningWithMessageInfo(
        //     EventId eventId,
        //     Exception exception,
        //     string logMessage,
        //     IRawBrokerEnvelope envelope);
        //
        // /// <summary>
        // ///     Writes a warning log message, enriching it with the information related to the provided message(s).
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="exception">
        // ///     The exception to log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelopes">
        // ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        // ///     log.
        // /// </param>
        // void LogWarningWithMessageInfo(
        //     EventId eventId,
        //     Exception exception,
        //     string logMessage,
        //     IReadOnlyCollection<IRawBrokerEnvelope> envelopes);
        //
        // /// <summary>
        // ///     Writes an error log message, enriching it with the information related to the provided message.
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelope">
        // ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        // /// </param>
        // void LogErrorWithMessageInfo(
        //     EventId eventId,
        //     string logMessage,
        //     IRawBrokerEnvelope envelope);
        //
        // /// <summary>
        // ///     Writes an error log message, enriching it with the information related to the provided message(s).
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelopes">
        // ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        // ///     log.
        // /// </param>
        // void LogErrorWithMessageInfo(
        //     EventId eventId,
        //     string logMessage,
        //     IReadOnlyCollection<IRawBrokerEnvelope> envelopes);
        //
        // /// <summary>
        // ///     Writes an error log message, enriching it with the information related to the provided message.
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="exception">
        // ///     The exception to log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelope">
        // ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        // /// </param>
        // void LogErrorWithMessageInfo(
        //     EventId eventId,
        //     Exception exception,
        //     string logMessage,
        //     IRawBrokerEnvelope envelope);
        //
        // /// <summary>
        // ///     Writes an error log message, enriching it with the information related to the provided message(s).
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="exception">
        // ///     The exception to log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelopes">
        // ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        // ///     log.
        // /// </param>
        // void LogErrorWithMessageInfo(
        //     EventId eventId,
        //     Exception exception,
        //     string logMessage,
        //     IReadOnlyCollection<IRawBrokerEnvelope> envelopes);
        //
        // /// <summary>
        // ///     Writes a critical log message, enriching it with the information related to the provided message.
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelope">
        // ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        // /// </param>
        // void LogCriticalWithMessageInfo(
        //     EventId eventId,
        //     string logMessage,
        //     IRawBrokerEnvelope envelope);
        //
        // /// <summary>
        // ///     Writes an critical log message, enriching it with the information related to the provided message(s).
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelopes">
        // ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        // ///     log.
        // /// </param>
        // void LogCriticalWithMessageInfo(
        //     EventId eventId,
        //     string logMessage,
        //     IReadOnlyCollection<IRawBrokerEnvelope> envelopes);
        //
        // /// <summary>
        // ///     Writes an critical log message, enriching it with the information related to the provided message.
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="exception">
        // ///     The exception to log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelope">
        // ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        // /// </param>
        // void LogCriticalWithMessageInfo(
        //     EventId eventId,
        //     Exception exception,
        //     string logMessage,
        //     IRawBrokerEnvelope envelope);
        //
        // /// <summary>
        // ///     Writes a critical log message, enriching it with the information related to the provided message(s).
        // /// </summary>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="exception">
        // ///     The exception to log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelopes">
        // ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        // ///     log.
        // /// </param>
        // void LogCriticalWithMessageInfo(
        //     EventId eventId,
        //     Exception exception,
        //     string logMessage,
        //     IReadOnlyCollection<IRawBrokerEnvelope> envelopes);
        //
        // /// <summary>
        // ///     Writes a log message at the specified log level, enriching it with the information related to the
        // ///     provided message(s).
        // /// </summary>
        // /// <param name="logLevel">
        // ///     Entry will be written on this level.
        // /// </param>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="exception">
        // ///     The exception to log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelope">
        // ///     The <see cref="IRawBrokerEnvelope" /> containing the message related to the this log.
        // /// </param>
        // void LogWithMessageInfo(
        //     LogLevel logLevel,
        //     EventId eventId,
        //     Exception? exception,
        //     string logMessage,
        //     IRawBrokerEnvelope envelope);
        //
        // /// <summary>
        // ///     Writes a log message at the specified log level, enriching it with the information related to the
        // ///     provided message(s).
        // /// </summary>
        // /// <param name="logLevel">
        // ///     Entry will be written on this level.
        // /// </param>
        // /// <param name="eventId">
        // ///     The event id associated with the log.
        // /// </param>
        // /// <param name="exception">
        // ///     The exception to log.
        // /// </param>
        // /// <param name="logMessage">
        // ///     The log message.
        // /// </param>
        // /// <param name="envelopes">
        // ///     The collection of <see cref="IRawBrokerEnvelope" /> containing the message(s) related to the this
        // ///     log.
        // /// </param>
        // void LogWithMessageInfo(
        //     LogLevel logLevel,
        //     EventId eventId,
        //     Exception? exception,
        //     string logMessage,
        //     IReadOnlyCollection<IRawBrokerEnvelope> envelopes);
    }
}
