// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     An <see cref="ILogger{TCategoryName}" /> with some specific methods to log inbound messages
    ///     related events.
    /// </summary>
    /// <typeparam name="TCategoryName">
    ///     The type who's name is used for the logger category name.
    /// </typeparam>
    public interface IInboundLogger<out TCategoryName> : ISilverbackLogger<TCategoryName>
    {
        /// <summary>
        ///     Logs the <see cref="IntegrationLogEvents.ProcessingInboundMessage" /> event.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="IRawInboundEnvelope" />.
        /// </param>
        void LogProcessing(IRawInboundEnvelope envelope);

        /// <summary>
        ///     Logs the <see cref="IntegrationLogEvents.ErrorProcessingInboundMessage" /> event.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="IRawInboundEnvelope" />.
        /// </param>
        /// <param name="exception">
        ///     The <see cref="Exception" />.
        /// </param>
        void LogProcessingError(IRawInboundEnvelope envelope, Exception exception);

        /// <summary>
        ///     Logs the <see cref="IntegrationLogEvents.ConsumerFatalError" /> event.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="IRawInboundEnvelope" />.
        /// </param>
        /// <param name="exception">
        ///     The <see cref="Exception" />.
        /// </param>
        void LogProcessingFatalError(IRawInboundEnvelope envelope, Exception exception);

        /// <summary>
        ///     Logs the <see cref="IntegrationLogEvents.RetryMessageProcessing" /> event.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="IRawInboundEnvelope" />.
        /// </param>
        void LogRetryProcessing(IRawInboundEnvelope envelope);

        /// <summary>
        ///     Logs the <see cref="IntegrationLogEvents.MessageMoved" /> event.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="IRawInboundEnvelope" />.
        /// </param>
        /// <param name="targetEndpoint">
        ///     The target <see cref="IProducerEndpoint" />.
        /// </param>
        void LogMoved(IRawInboundEnvelope envelope, IProducerEndpoint targetEndpoint);

        /// <summary>
        ///     Logs the <see cref="IntegrationLogEvents.MessageSkipped" /> event.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="IRawInboundEnvelope" />.
        /// </param>
        void LogSkipped(IRawInboundEnvelope envelope);

        /// <summary>
        ///     Logs the <see cref="IntegrationLogEvents.CannotMoveSequences" /> event.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="IRawInboundEnvelope" />.
        /// </param>
        /// <param name="sequence">
        ///     The <see cref="ISequence" />.
        /// </param>
        void LogCannotMoveSequences(IRawInboundEnvelope envelope, ISequence sequence);

        /// <summary>
        ///     Logs the <see cref="IntegrationLogEvents.RollbackToRetryFailed" /> event.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="IRawInboundEnvelope" />.
        /// </param>
        /// <param name="exception">
        ///     The <see cref="Exception" />.
        /// </param>
        void LogRollbackToRetryFailed(IRawInboundEnvelope envelope, Exception exception);

        /// <summary>
        ///     Logs the <see cref="IntegrationLogEvents.RollbackToSkipFailed" /> event.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="IRawInboundEnvelope" />.
        /// </param>
        /// <param name="exception">
        ///     The <see cref="Exception" />.
        /// </param>
        void LogRollbackToSkipFailed(IRawInboundEnvelope envelope, Exception exception);

        /// <summary>
        ///     Logs the <see cref="IntegrationLogEvents.MessageAlreadyProcessed" /> event.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="IRawInboundEnvelope" />.
        /// </param>
        void LogAlreadyProcessed(IRawInboundEnvelope envelope);

        /// <summary>
        ///     Logs the <see cref="IntegrationLogEvents.LowLevelTracing" /> event.
        /// </summary>
        /// <param name="logEvent">
        ///     The <see cref="LogEvent" />.
        /// </param>
        /// <param name="envelope">
        ///     The <see cref="IRawInboundEnvelope" />.
        /// </param>
        /// <param name="argumentsProvider">
        ///     The <see cref="Func{TResult}" /> returning the arguments to be used to format the message.
        /// </param>
        /// <remarks>
        ///     This method is less performing and therefore only events with <see cref="LogLevel.Trace" /> level are
        ///     allowed.
        /// </remarks>
        void LogInboundTrace(
            LogEvent logEvent,
            IRawInboundEnvelope envelope,
            Func<object?[]>? argumentsProvider = null);

        /// <summary>
        ///     Logs the <see cref="IntegrationLogEvents.LowLevelTracing" /> event.
        /// </summary>
        /// <param name="logEvent">
        ///     The <see cref="LogEvent" />.
        /// </param>
        /// <param name="envelope">
        ///     The <see cref="IRawInboundEnvelope" />.
        /// </param>
        /// <param name="exception">
        ///     The <see cref="Exception" /> to be logged.
        /// </param>
        /// <param name="argumentsProvider">
        ///     The <see cref="Func{TResult}" /> returning the arguments to be used to format the message.
        /// </param>
        /// <remarks>
        ///     This method is less performing and therefore only events with <see cref="LogLevel.Trace" /> level are
        ///     allowed.
        /// </remarks>
        void LogInboundTrace(
            LogEvent logEvent,
            IRawInboundEnvelope envelope,
            Exception? exception,
            Func<object?[]>? argumentsProvider = null);

        /// <summary>
        ///     Logs the <see cref="IntegrationLogEvents.LowLevelTracing" /> event.
        /// </summary>
        /// <param name="message">
        ///     The log message format string.
        /// </param>
        /// <param name="envelope">
        ///     The <see cref="IRawInboundEnvelope" />.
        /// </param>
        /// <param name="argumentsProvider">
        ///     The <see cref="Func{TResult}" /> returning the arguments to be used to format the message.
        /// </param>
        void LogInboundLowLevelTrace(
            string message,
            IRawInboundEnvelope envelope,
            Func<object?[]>? argumentsProvider = null);

        /// <summary>
        ///     Logs the <see cref="IntegrationLogEvents.LowLevelTracing" /> event.
        /// </summary>
        /// <param name="message">
        ///     The log message format string.
        /// </param>
        /// <param name="envelope">
        ///     The <see cref="IRawInboundEnvelope" />.
        /// </param>
        /// <param name="exception">
        ///     The <see cref="Exception" /> to be logged.
        /// </param>
        /// <param name="argumentsProvider">
        ///     The <see cref="Func{TResult}" /> returning the arguments to be used to format the message.
        /// </param>
        void LogInboundLowLevelTrace(
            string message,
            IRawInboundEnvelope envelope,
            Exception? exception,
            Func<object?[]>? argumentsProvider = null);
    }
}
