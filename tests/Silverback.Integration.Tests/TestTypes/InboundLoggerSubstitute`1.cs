// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;

namespace Silverback.Tests.Integration.TestTypes;

public class InboundLoggerSubstitute<TCategory> : IInboundLogger<TCategory>
{
    public ILogger InnerLogger { get; } = Substitute.For<ILogger>();

    public bool IsEnabled(LogEvent logEvent) => true;

    public void LogProcessing(IRawInboundEnvelope envelope)
    {
    }

    public void LogProcessingError(IRawInboundEnvelope envelope, Exception exception)
    {
    }

    public void LogProcessingFatalError(IRawInboundEnvelope envelope, Exception exception)
    {
    }

    public void LogRetryProcessing(IRawInboundEnvelope envelope)
    {
    }

    public void LogMoved(IRawInboundEnvelope envelope, ProducerConfiguration producerConfiguration)
    {
    }

    public void LogSkipped(IRawInboundEnvelope envelope)
    {
    }

    public void LogCannotMoveSequences(IRawInboundEnvelope envelope, ISequence sequence)
    {
    }

    public void LogRollbackToRetryFailed(IRawInboundEnvelope envelope, Exception exception)
    {
    }

    public void LogRollbackToSkipFailed(IRawInboundEnvelope envelope, Exception exception)
    {
    }

    public void LogNullMessageSkipped(IRawInboundEnvelope envelope)
    {
    }

    public void LogAlreadyProcessed(IRawInboundEnvelope envelope)
    {
    }

    public void LogInboundTrace(LogEvent logEvent, IRawInboundEnvelope envelope, Func<object?[]>? argumentsProvider = null)
    {
    }

    public void LogInboundTrace(
        LogEvent logEvent,
        IRawInboundEnvelope envelope,
        Exception? exception,
        Func<object?[]>? argumentsProvider = null)
    {
    }

    public void LogInboundLowLevelTrace(string message, IRawInboundEnvelope envelope, Func<object?[]>? argumentsProvider = null)
    {
    }

    public void LogInboundLowLevelTrace(
        string message,
        IRawInboundEnvelope envelope,
        Exception? exception,
        Func<object?[]>? argumentsProvider = null)
    {
    }
}
