// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Messaging.Sequences;

namespace Silverback.Diagnostics;

internal static class IntegrationLoggerExtensions
{
    private static readonly Action<ILogger, string, string, Exception?> ProcessingConsumedMessage =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.ProcessingConsumedMessage);

    private static readonly Action<ILogger, string, string, Exception?> ProcessingConsumedMessageError =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.ProcessingConsumedMessageError);

    private static readonly Action<ILogger, string, string, Exception?> ProcessingConsumedMessageFatalError =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.ProcessingConsumedMessageFatalError);

    private static readonly Action<ILogger, string, Exception?> ConsumerFatalError =
        SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.ConsumerFatalError);

    private static readonly Action<ILogger, string, string?, Exception?> MessageProduced =
        SilverbackLoggerMessage.Define<string, string?>(IntegrationLogEvents.MessageProduced);

    private static readonly Action<ILogger, string, Exception?> ErrorProducingMessage =
        SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.ErrorProducingMessage);

    private static readonly Action<ILogger, string, Exception?> OutboundMessageFiltered =
        SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.OutboundMessageFiltered);

    private static readonly Action<ILogger, string?, string, string, int, Exception?> MessageAddedToSequence =
        SilverbackLoggerMessage.Define<string?, string, string, int>(IntegrationLogEvents.MessageAddedToSequence);

    private static readonly Action<ILogger, string, string, Exception?> SequenceStarted =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.SequenceStarted);

    private static readonly Action<ILogger, string, string, int, Exception?> SequenceCompleted =
        SilverbackLoggerMessage.Define<string, string, int>(IntegrationLogEvents.SequenceCompleted);

    private static readonly Action<ILogger, string, string, int, SequenceAbortReason, Exception?> SequenceProcessingAborted =
        SilverbackLoggerMessage.Define<string, string, int, SequenceAbortReason>(IntegrationLogEvents.SequenceProcessingAborted);

    private static readonly Action<ILogger, string, string, int, Exception?> SequenceProcessingError =
        SilverbackLoggerMessage.Define<string, string, int>(IntegrationLogEvents.SequenceProcessingError);

    private static readonly Action<ILogger, string, string, int, Exception?> IncompleteSequenceAborted =
        SilverbackLoggerMessage.Define<string, string, int>(IntegrationLogEvents.IncompleteSequenceAborted);

    private static readonly Action<ILogger, string, Exception?> IncompleteSequenceSkipped =
        SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.IncompleteSequenceSkipped);

    private static readonly Action<ILogger, string, string, Exception?> SequenceTimeoutError =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.SequenceTimeoutError);

    private static readonly Action<ILogger, Exception?> BrokerClientsInitializationError =
        SilverbackLoggerMessage.Define(IntegrationLogEvents.BrokerClientsInitializationError);

    private static readonly Action<ILogger, string, string, Exception?> BrokerClientInitializing =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.BrokerClientInitializing);

    private static readonly Action<ILogger, string, string, Exception?> BrokerClientInitialized =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.BrokerClientInitialized);

    private static readonly Action<ILogger, string, string, Exception?> BrokerClientDisconnecting =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.BrokerClientDisconnecting);

    private static readonly Action<ILogger, string, string, Exception?> BrokerClientDisconnected =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.BrokerClientDisconnected);

    private static readonly Action<ILogger, string, string, Exception?> BrokerClientInitializeError =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.BrokerClientInitializeError);

    private static readonly Action<ILogger, string, string, Exception?> BrokerClientDisconnectError =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.BrokerClientDisconnectError);

    private static readonly Action<ILogger, string, double, string, Exception?> BrokerClientReconnectError =
        SilverbackLoggerMessage.Define<string, double, string>(IntegrationLogEvents.BrokerClientReconnectError);

    private static readonly Action<ILogger, string, string, Exception?> ConsumerStartError =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.ConsumerStartError);

    private static readonly Action<ILogger, string, string, Exception?> ConsumerStopError =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.ConsumerStopError);

    private static readonly Action<ILogger, string, string, string, Exception?> ConsumerCommitError =
        SilverbackLoggerMessage.Define<string, string, string>(IntegrationLogEvents.ConsumerCommitError);

    private static readonly Action<ILogger, string, string, string, Exception?> ConsumerRollbackError =
        SilverbackLoggerMessage.Define<string, string, string>(IntegrationLogEvents.ConsumerRollbackError);

    private static readonly Action<ILogger, string, string, Exception?> BrokerClientCreated =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.BrokerClientCreated);

    private static readonly Action<ILogger, string, string, Exception?> ConsumerCreated =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.ConsumerCreated);

    private static readonly Action<ILogger, string, string, Exception?> ProducerCreated =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.ProducerCreated);

    private static readonly Action<ILogger, string, string, Exception?> RetryMessageProcessing =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.RetryMessageProcessing);

    private static readonly Action<ILogger, string, string, string, Exception?> MessageMoved =
        SilverbackLoggerMessage.Define<string, string, string>(IntegrationLogEvents.MessageMoved);

    private static readonly Action<ILogger, string, string, Exception?> MessageSkipped =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.MessageSkipped);

    private static readonly Action<ILogger, string, string, string, Exception?> CannotMoveSequences =
        SilverbackLoggerMessage.Define<string, string, string>(IntegrationLogEvents.CannotMoveSequence);

    private static readonly Action<ILogger, string, string, Exception?> RollbackToRetryFailed =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.RollbackToRetryFailed);

    private static readonly Action<ILogger, string, string, Exception?> RollbackToSkipFailed =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.RollbackToSkipFailed);

    private static readonly Action<ILogger, string, Exception?> StoringIntoOutbox =
        SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.StoringIntoOutbox);

    private static readonly Action<ILogger, int, Exception?> ReadingMessagesFromOutbox =
        SilverbackLoggerMessage.Define<int>(IntegrationLogEvents.ReadingMessagesFromOutbox);

    private static readonly Action<ILogger, Exception?> OutboxEmpty =
        SilverbackLoggerMessage.Define(IntegrationLogEvents.OutboxEmpty);

    private static readonly Action<ILogger, int, Exception?> ProcessingOutboxStoredMessage =
        SilverbackLoggerMessage.Define<int>(IntegrationLogEvents.ProcessingOutboxStoredMessage);

    private static readonly Action<ILogger, string, Exception?> ErrorProducingOutboxStoredMessage =
        SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.ErrorProducingOutboxStoredMessage);

    private static readonly Action<ILogger, Exception?> ErrorProcessingOutbox =
        SilverbackLoggerMessage.Define(IntegrationLogEvents.ErrorProcessingOutbox);

    private static readonly Action<ILogger, string, string, Exception?> InvalidMessageProduced =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.InvalidMessageProduced);

    private static readonly Action<ILogger, string, string, string, Exception?> InvalidMessageConsumed =
        SilverbackLoggerMessage.Define<string, string, string>(IntegrationLogEvents.InvalidMessageConsumed);

    private static readonly Action<ILogger, string, Exception?> InvalidEndpointConfiguration =
        SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.InvalidEndpointConfiguration);

    private static readonly Action<ILogger, string, Exception?> EndpointConfiguratorError =
        SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.EndpointConfiguratorError);

    private static readonly Action<ILogger, Exception?> CallbackError =
        SilverbackLoggerMessage.Define(IntegrationLogEvents.CallbackError);

    private static readonly Action<ILogger, string?, Exception?> EndpointBuilderError =
        SilverbackLoggerMessage.Define<string?>(IntegrationLogEvents.EndpointBuilderError);

    public static void LogProcessing(this ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.ProcessingConsumedMessage))
            return;

        ProcessingConsumedMessage(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString(),
            null);
    }

    public static void LogProcessingError(this ISilverbackLogger logger, IRawInboundEnvelope envelope, Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.ProcessingConsumedMessageError))
            return;

        ProcessingConsumedMessageError(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString(),
            exception);
    }

    public static void LogProcessingFatalError(
        this ISilverbackLogger logger,
        IRawInboundEnvelope envelope,
        Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.ProcessingConsumedMessageFatalError))
            return;

        ProcessingConsumedMessageFatalError(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString(),
            exception);
    }

    public static void LogConsumerFatalError(this ISilverbackLogger logger, IConsumer consumer, Exception exception) =>
        ConsumerFatalError(logger.InnerLogger, consumer.DisplayName, exception);

    public static void LogProduced(this ISilverbackLogger logger, IOutboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.MessageProduced))
            return;

        MessageProduced(
            logger.InnerLogger,
            envelope.EndpointConfiguration.DisplayName,
            envelope.BrokerMessageIdentifier?.ToLogString(),
            null);
    }

    public static void LogProduced(
        this ISilverbackLogger logger,
        EndpointConfiguration endpointConfiguration,
        IBrokerMessageIdentifier? brokerMessageIdentifier)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.MessageProduced))
            return;

        MessageProduced(
            logger.InnerLogger,
            endpointConfiguration.DisplayName,
            brokerMessageIdentifier?.ToLogString(),
            null);
    }

    public static void LogProduceError(this ISilverbackLogger logger, IOutboundEnvelope envelope, Exception exception) =>
        ErrorProducingMessage(logger.InnerLogger, envelope.EndpointConfiguration.DisplayName, exception);

    public static void LogProduceError(this ISilverbackLogger logger, ProducerEndpointConfiguration endpointConfiguration, Exception exception) =>
        ErrorProducingMessage(logger.InnerLogger, endpointConfiguration.DisplayName, exception);

    public static void LogOutboundMessageFiltered(this ISilverbackLogger logger, IOutboundEnvelope envelope) =>
        OutboundMessageFiltered(logger.InnerLogger, envelope.EndpointConfiguration.DisplayName, null);

    public static void LogMessageAddedToSequence(
        this ISilverbackLogger logger,
        IRawInboundEnvelope envelope,
        ISequence sequence)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.MessageAddedToSequence))
            return;

        MessageAddedToSequence(
            logger.InnerLogger,
            envelope.BrokerMessageIdentifier.ToLogString(),
            sequence.GetType().Name,
            sequence.SequenceId,
            sequence.Length,
            null);
    }

    public static void LogSequenceStarted(
        this ISilverbackLogger logger,
        ISequence sequence)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.SequenceStarted))
            return;

        SequenceStarted(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            null);
    }

    public static void LogSequenceCompleted(
        this ISilverbackLogger logger,
        ISequence sequence)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.SequenceCompleted))
            return;

        SequenceCompleted(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            sequence.Length,
            null);
    }

    public static void LogSequenceAborted(
        this ISilverbackLogger logger,
        ISequence sequence,
        SequenceAbortReason reason)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.SequenceProcessingAborted))
            return;

        SequenceProcessingAborted(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            sequence.Length,
            reason,
            null);
    }

    public static void LogSequenceProcessingError(this ISilverbackLogger logger, ISequence sequence, Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.SequenceProcessingError))
            return;

        SequenceProcessingError(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            sequence.Length,
            exception);
    }

    public static void LogIncompleteSequenceAborted(this ISilverbackLogger logger, ISequence sequence)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.IncompleteSequenceAborted))
            return;

        IncompleteSequenceAborted(
            logger.InnerLogger,
            sequence.GetType().Name,
            sequence.SequenceId,
            sequence.Length,
            null);
    }

    public static void LogIncompleteSequenceSkipped(this ISilverbackLogger logger, IncompleteSequence sequence) =>
        IncompleteSequenceSkipped(logger.InnerLogger, sequence.SequenceId, null);

    public static void LogSequenceTimeoutError(this ISilverbackLogger logger, ISequence sequence, Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.SequenceTimeoutError))
            return;

        SequenceTimeoutError(logger.InnerLogger, sequence.GetType().Name, sequence.SequenceId, exception);
    }

    public static void LogBrokerClientsInitializationError(this ISilverbackLogger logger, Exception exception) =>
        BrokerClientsInitializationError(logger.InnerLogger, exception);

    public static void LogBrokerClientInitializing(this ISilverbackLogger logger, IBrokerClient brokerClient) =>
        BrokerClientInitializing(logger.InnerLogger, brokerClient.GetType().Name, brokerClient.DisplayName, null);

    public static void LogBrokerClientInitialized(this ISilverbackLogger logger, IBrokerClient brokerClient) =>
        BrokerClientInitialized(logger.InnerLogger, brokerClient.GetType().Name, brokerClient.DisplayName, null);

    public static void LogBrokerClientDisconnecting(this ISilverbackLogger logger, IBrokerClient brokerClient) =>
        BrokerClientDisconnecting(logger.InnerLogger, brokerClient.GetType().Name, brokerClient.DisplayName, null);

    public static void LogBrokerClientDisconnected(this ISilverbackLogger logger, IBrokerClient brokerClient) =>
        BrokerClientDisconnected(logger.InnerLogger, brokerClient.GetType().Name, brokerClient.DisplayName, null);

    public static void LogBrokerClientInitializeError(this ISilverbackLogger logger, IBrokerClient brokerClient, Exception exception) =>
        BrokerClientInitializeError(logger.InnerLogger, brokerClient.GetType().Name, brokerClient.DisplayName, exception);

    public static void LogBrokerClientDisconnectError(this ISilverbackLogger logger, IBrokerClient brokerClient, Exception exception) =>
        BrokerClientDisconnectError(logger.InnerLogger, brokerClient.GetType().Name, brokerClient.DisplayName, exception);

    public static void LogBrokerClientReconnectError(
        this ISilverbackLogger logger,
        IBrokerClient brokerClient,
        TimeSpan retryDelay,
        Exception exception) =>
        BrokerClientReconnectError(
            logger.InnerLogger,
            brokerClient.GetType().Name,
            retryDelay.TotalMilliseconds,
            brokerClient.DisplayName,
            exception);

    public static void LogConsumerStartError(this ISilverbackLogger logger, IConsumer consumer, Exception exception) =>
        ConsumerStartError(logger.InnerLogger, consumer.GetType().Name, consumer.DisplayName, exception);

    public static void LogConsumerStopError(this ISilverbackLogger logger, IConsumer consumer, Exception exception) =>
        ConsumerStopError(logger.InnerLogger, consumer.GetType().Name, consumer.DisplayName, exception);

    public static void LogConsumerCommitError(
        this ISilverbackLogger logger,
        IConsumer consumer,
        IReadOnlyCollection<IBrokerMessageIdentifier> identifiers,
        Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.ConsumerCommitError))
            return;

        ConsumerCommitError(
            logger.InnerLogger,
            consumer.GetType().Name,
            consumer.DisplayName,
            string.Join(", ", identifiers.Select(identifier => identifier.ToVerboseLogString())),
            exception);
    }

    public static void LogConsumerRollbackError(
        this ISilverbackLogger logger,
        IConsumer consumer,
        IReadOnlyCollection<IBrokerMessageIdentifier> identifiers,
        Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.ConsumerRollbackError))
            return;

        ConsumerRollbackError(
            logger.InnerLogger,
            consumer.GetType().Name,
            consumer.DisplayName,
            string.Join(", ", identifiers.Select(identifier => identifier.ToVerboseLogString())),
            exception);
    }

    public static void LogBrokerClientCreated(this ISilverbackLogger logger, IBrokerClient brokerClient) =>
        BrokerClientCreated(logger.InnerLogger, brokerClient.GetType().Name, brokerClient.DisplayName, null);

    public static void LogConsumerCreated(this ISilverbackLogger logger, IConsumer consumer) =>
        ConsumerCreated(logger.InnerLogger, consumer.GetType().Name, consumer.DisplayName, null);

    public static void LogProducerCreated(this ISilverbackLogger logger, IProducer producer) =>
        ProducerCreated(logger.InnerLogger, producer.GetType().Name, producer.DisplayName, null);

    public static void LogRetryProcessing(this ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.RetryMessageProcessing))
            return;

        RetryMessageProcessing(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString(),
            null);
    }

    public static void LogMessageMoved(
        this ISilverbackLogger logger,
        IRawInboundEnvelope envelope,
        EndpointConfiguration destinationEndpoint)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.MessageMoved))
            return;

        MessageMoved(
            logger.InnerLogger,
            destinationEndpoint.DisplayName,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString(),
            null);
    }

    public static void LogMessageSkipped(this ISilverbackLogger logger, IRawInboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.MessageSkipped))
            return;

        MessageSkipped(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString(),
            null);
    }

    public static void LogCannotMoveSequence(this ISilverbackLogger logger, IRawInboundEnvelope envelope, ISequence sequence)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.CannotMoveSequence))
            return;

        CannotMoveSequences(
            logger.InnerLogger,
            sequence.GetType().Name,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString(),
            null);
    }

    public static void LogRollbackToRetryFailed(this ISilverbackLogger logger, IRawInboundEnvelope envelope, Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.RollbackToRetryFailed))
            return;

        RollbackToRetryFailed(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString(),
            exception);
    }

    public static void LogRollbackToSkipFailed(this ISilverbackLogger logger, IRawInboundEnvelope envelope, Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.RollbackToSkipFailed))
            return;

        RollbackToSkipFailed(
            logger.InnerLogger,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString(),
            exception);
    }

    public static void LogStoringIntoOutbox(this ISilverbackLogger logger, IOutboundEnvelope envelope) =>
        StoringIntoOutbox(
            logger.InnerLogger,
            envelope.EndpointConfiguration.DisplayName,
            null);

    public static void LogReadingMessagesFromOutbox(this ISilverbackLogger logger, int packageSize) =>
        ReadingMessagesFromOutbox(logger.InnerLogger, packageSize, null);

    public static void LogOutboxEmpty(this ISilverbackLogger logger) =>
        OutboxEmpty(logger.InnerLogger, null);

    public static void LogProcessingOutboxStoredMessage(this ISilverbackLogger logger, int currentIndex) =>
        ProcessingOutboxStoredMessage(logger.InnerLogger, currentIndex, null);

    public static void LogErrorProducingOutboxStoredMessage(
        this ISilverbackLogger logger,
        OutboxMessage message,
        Exception exception) =>
        ErrorProducingOutboxStoredMessage(logger.InnerLogger, message.EndpointName, exception);

    public static void LogErrorProcessingOutbox(this ISilverbackLogger logger, Exception exception) =>
        ErrorProcessingOutbox(logger.InnerLogger, exception);

    public static void LogInvalidMessageProduced(
        this ISilverbackLogger logger,
        IOutboundEnvelope envelope,
        string validationErrors) =>
        InvalidMessageProduced(
            logger.InnerLogger,
            validationErrors,
            envelope.EndpointConfiguration.DisplayName,
            null);

    public static void LogInvalidMessageConsumed(
        this ISilverbackLogger logger,
        IRawInboundEnvelope envelope,
        string validationErrors)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.InvalidMessageConsumed))
            return;

        InvalidMessageConsumed(
            logger.InnerLogger,
            validationErrors,
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString(),
            null);
    }

    public static void LogInvalidEndpointConfiguration(
        this ISilverbackLogger logger,
        EndpointConfiguration endpointConfiguration,
        Exception exception) =>
        InvalidEndpointConfiguration(logger.InnerLogger, endpointConfiguration.DisplayName, exception);

    public static void LogEndpointConfiguratorError(
        this ISilverbackLogger logger,
        IBrokerClientsConfigurator configurator,
        Exception exception) =>
        EndpointConfiguratorError(logger.InnerLogger, configurator.GetType().Name, exception);

    public static void LogCallbackError(this ISilverbackLogger logger, Exception exception) =>
        CallbackError(logger.InnerLogger, exception);

    public static void LogEndpointBuilderError(this ISilverbackLogger logger, string? endpointName, Exception exception) =>
        EndpointBuilderError(logger.InnerLogger, endpointName, exception);

    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem", Justification = "Optimized via IsEnabled")]
    [SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Optimized via IsEnabled")]
    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Optimized via IsEnabled")]
    public static void LogTrace(this ISilverbackLogger logger, string message, Func<object[]>? argumentsProvider = null)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        logger.InnerLogger.Log(
            IntegrationLogEvents.Tracing.Level,
            IntegrationLogEvents.Tracing.EventId,
            message,
            argumentsProvider?.Invoke() ?? []);
    }

    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem", Justification = "Optimized via IsEnabled")]
    [SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Optimized via IsEnabled")]
    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Optimized via IsEnabled")]
    public static void LogTrace(
        this ISilverbackLogger logger,
        Exception? exception,
        string message,
        Func<object[]>? argumentsProvider = null)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        logger.InnerLogger.Log(
            IntegrationLogEvents.Tracing.Level,
            IntegrationLogEvents.Tracing.EventId,
            exception,
            message,
            argumentsProvider?.Invoke() ?? []);
    }

    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem", Justification = "Optimized via IsEnabled")]
    [SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Optimized via IsEnabled")]
    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Optimized via IsEnabled")]
    public static void LogConsumerTrace(
        this ISilverbackLogger logger,
        IConsumer? consumer,
        string message,
        Func<object?[]>? argumentsProvider = null)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        object?[] args =
        [
            ..argumentsProvider?.Invoke() ?? [],
            consumer?.DisplayName ?? string.Empty
        ];

        logger.InnerLogger.Log(
            IntegrationLogEvents.Tracing.Level,
            IntegrationLogEvents.Tracing.EventId,
            message + " | ConsumerName: {ConsumerName}",
            args);
    }

    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem", Justification = "Optimized via IsEnabled")]
    [SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Optimized via IsEnabled")]
    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Optimized via IsEnabled")]
    public static void LogProcessingTrace(
        this ISilverbackLogger logger,
        IRawInboundEnvelope envelope,
        string message,
        Func<object?[]>? argumentsProvider = null)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        object?[] args =
        [
            ..argumentsProvider?.Invoke() ?? [],
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString()
        ];

        logger.InnerLogger.Log(
            IntegrationLogEvents.Tracing.Level,
            IntegrationLogEvents.Tracing.EventId,
            message + " | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
            args);
    }

    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem", Justification = "Optimized via IsEnabled")]
    [SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Optimized via IsEnabled")]
    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Optimized via IsEnabled")]
    public static void LogProcessingTrace(
        this ISilverbackLogger logger,
        IRawInboundEnvelope envelope,
        Exception? exception,
        string message,
        Func<object?[]>? argumentsProvider = null)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.Tracing))
            return;

        object?[] args =
        [
            ..argumentsProvider?.Invoke() ?? [],
            envelope.Endpoint.DisplayName,
            envelope.BrokerMessageIdentifier.ToLogString()
        ];

        logger.InnerLogger.Log(
            IntegrationLogEvents.Tracing.Level,
            IntegrationLogEvents.Tracing.EventId,
            exception,
            message + " | EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId}",
            args);
    }
}
