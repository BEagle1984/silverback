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
        private readonly LoggerCollection _loggers;

        public InboundLogger(IMappedLevelsLogger<TCategoryName> mappedLevelsLogger, LoggerCollection loggers)
            : base(mappedLevelsLogger)
        {
            _loggers = Check.NotNull(loggers, nameof(loggers));
        }

        public void LogProcessing(IRawInboundEnvelope envelope) =>
            GetLogger(envelope).LogProcessing(this, envelope);

        public void LogProcessingError(IRawInboundEnvelope envelope, Exception exception) =>
            GetLogger(envelope).LogProcessingError(this, envelope, exception);

        public void LogProcessingFatalError(IRawInboundEnvelope envelope, Exception exception) =>
            GetLogger(envelope).LogProcessingFatalError(this, envelope, exception);

        public void LogRetryProcessing(IRawInboundEnvelope envelope) =>
            GetLogger(envelope).LogRetryProcessing(this, envelope);

        public void LogMoved(IRawInboundEnvelope envelope, IProducerEndpoint targetEndpoint) =>
            GetLogger(envelope).LogMoved(this, envelope, targetEndpoint);

        public void LogSkipped(IRawInboundEnvelope envelope) =>
            GetLogger(envelope).LogSkipped(this, envelope);

        public void LogCannotMoveSequences(IRawInboundEnvelope envelope, ISequence sequence) =>
            GetLogger(envelope).LogCannotMoveSequences(this, envelope, sequence);

        public void LogRollbackToRetryFailed(IRawInboundEnvelope envelope, Exception exception) =>
            GetLogger(envelope).LogRollbackToRetryFailed(this, envelope, exception);

        public void LogRollbackToSkipFailed(IRawInboundEnvelope envelope, Exception exception) =>
            GetLogger(envelope).LogRollbackToSkipFailed(this, envelope, exception);

        public void LogNullMessageSkipped(IRawInboundEnvelope envelope) =>
            GetLogger(envelope).LogNullMessageSkipped(this, envelope);

        public void LogAlreadyProcessed(IRawInboundEnvelope envelope) =>
            GetLogger(envelope).LogAlreadyProcessed(this, envelope);

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

            GetLogger(envelope).LogInboundTrace(
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
            GetLogger(envelope).LogInboundTrace(
                this,
                IntegrationLogEvents.LowLevelTracing.Level,
                IntegrationLogEvents.LowLevelTracing.EventId,
                message,
                envelope,
                exception,
                argumentsProvider);

        private InboundLogger GetLogger(IRawInboundEnvelope envelope) =>
            _loggers.GetInboundLogger(envelope.Endpoint.GetType());
    }
}
