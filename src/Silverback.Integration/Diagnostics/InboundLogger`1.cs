// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Diagnostics
{
    internal class InboundLogger<TCategoryName>
        : SilverbackLogger<TCategoryName>, IInboundLogger<TCategoryName>
    {
        private readonly InboundLoggerFactory _loggerFactory;

        public InboundLogger(
            IMappedLevelsLogger<TCategoryName> mappedLevelsLogger,
            InboundLoggerFactory loggerFactory)
            : base(mappedLevelsLogger)
        {
            _loggerFactory = Check.NotNull(loggerFactory, nameof(loggerFactory));
        }

        public void LogProcessing(IRawInboundEnvelope envelope) =>
            _loggerFactory.GetInboundLogger(envelope.Endpoint)
                .LogProcessing(this, envelope);

        public void LogProcessingError(IRawInboundEnvelope envelope, Exception exception) =>
            _loggerFactory.GetInboundLogger(envelope.Endpoint)
                .LogProcessingError(this, envelope, exception);

        public void LogProcessingFatalError(IRawInboundEnvelope envelope, Exception exception) =>
            _loggerFactory.GetInboundLogger(envelope.Endpoint)
                .LogProcessingFatalError(this, envelope, exception);

        public void LogRetryProcessing(IRawInboundEnvelope envelope) =>
            _loggerFactory.GetInboundLogger(envelope.Endpoint)
                .LogRetryProcessing(this, envelope);

        public void LogMoved(IRawInboundEnvelope envelope, IProducerEndpoint targetEndpoint) =>
            _loggerFactory.GetInboundLogger(envelope.Endpoint)
                .LogMoved(this, envelope, targetEndpoint);

        public void LogSkipped(IRawInboundEnvelope envelope) =>
            _loggerFactory.GetInboundLogger(envelope.Endpoint)
                .LogSkipped(this, envelope);

        public void LogCannotMoveSequences(IRawInboundEnvelope envelope, ISequence sequence) =>
            _loggerFactory.GetInboundLogger(envelope.Endpoint)
                .LogCannotMoveSequences(this, envelope, sequence);

        public void LogRollbackToRetryFailed(IRawInboundEnvelope envelope, Exception exception) =>
            _loggerFactory.GetInboundLogger(envelope.Endpoint)
                .LogRollbackToRetryFailed(this, envelope, exception);

        public void LogRollbackToSkipFailed(IRawInboundEnvelope envelope, Exception exception) =>
            _loggerFactory.GetInboundLogger(envelope.Endpoint)
                .LogRollbackToSkipFailed(this, envelope, exception);

        public void LogNullMessageSkipped(IRawInboundEnvelope envelope) =>
            _loggerFactory.GetInboundLogger(envelope.Endpoint)
                .LogNullMessageSkipped(this, envelope);

        public void LogAlreadyProcessed(IRawInboundEnvelope envelope) =>
            _loggerFactory.GetInboundLogger(envelope.Endpoint)
                .LogAlreadyProcessed(this, envelope);

        public void LogInboundTrace(
            LogEvent logEvent,
            IRawInboundEnvelope envelope,
            Func<object?[]>? argumentsProvider = null) =>
            LogInboundTrace(logEvent, envelope, null, argumentsProvider);

        public void LogInboundTrace(
            LogEvent logEvent,
            IRawInboundEnvelope envelope,
            Exception? exception,
            Func<object?[]>? argumentsProvider = null)
        {
            if (logEvent.Level > LogLevel.Trace)
                throw new InvalidOperationException("This method is intended for tracing only.");

            _loggerFactory.GetInboundLogger(envelope.Endpoint).LogInboundTrace(
                this,
                logEvent.Level,
                logEvent.EventId,
                logEvent.Message,
                envelope,
                exception,
                argumentsProvider);
        }

        public void LogInboundLowLevelTrace(
            string message,
            IRawInboundEnvelope envelope,
            Func<object?[]>? argumentsProvider = null) =>
            LogInboundLowLevelTrace(message, envelope, null, argumentsProvider);

        public void LogInboundLowLevelTrace(
            string message,
            IRawInboundEnvelope envelope,
            Exception? exception,
            Func<object?[]>? argumentsProvider = null) =>
            _loggerFactory.GetInboundLogger(envelope.Endpoint).LogInboundTrace(
                this,
                IntegrationLogEvents.LowLevelTracing.Level,
                IntegrationLogEvents.LowLevelTracing.EventId,
                message,
                envelope,
                exception,
                argumentsProvider);
    }
}
