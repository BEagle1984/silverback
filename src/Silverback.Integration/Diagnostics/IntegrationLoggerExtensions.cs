// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;

namespace Silverback.Diagnostics;

internal static class IntegrationLoggerExtensions
{
    private static readonly Action<ILogger, string, Exception?> ConsumerFatalError =
        SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.ConsumerFatalError);

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

    private static readonly Action<ILogger, string, string, Exception?> BrokerClientCreated =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.BrokerClientCreated);

    private static readonly Action<ILogger, string, string, Exception?> ConsumerCreated =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.ConsumerCreated);

    private static readonly Action<ILogger, string, string, Exception?> ProducerCreated =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.ProducerCreated);

    private static readonly Action<ILogger, string, string, Exception?> ConsumerStartError =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.ConsumerStartError);

    private static readonly Action<ILogger, string, string, Exception?> ConsumerStopError =
        SilverbackLoggerMessage.Define<string, string>(IntegrationLogEvents.ConsumerStopError);

    private static readonly Action<ILogger, string, string, string, Exception?> ConsumerCommitError =
        SilverbackLoggerMessage.Define<string, string, string>(IntegrationLogEvents.ConsumerCommitError);

    private static readonly Action<ILogger, string, string, string, Exception?> ConsumerRollbackError =
        SilverbackLoggerMessage.Define<string, string, string>(IntegrationLogEvents.ConsumerRollbackError);

    private static readonly Action<ILogger, int, Exception?> ReadingMessagesFromOutbox =
        SilverbackLoggerMessage.Define<int>(IntegrationLogEvents.ReadingMessagesFromOutbox);

    private static readonly Action<ILogger, Exception?> OutboxEmpty =
        SilverbackLoggerMessage.Define(IntegrationLogEvents.OutboxEmpty);

    private static readonly Action<ILogger, int, Exception?> ProcessingOutboxStoredMessage =
        SilverbackLoggerMessage.Define<int>(IntegrationLogEvents.ProcessingOutboxStoredMessage);

    private static readonly Action<ILogger, Exception?> ErrorProducingOutboxStoredMessage =
        SilverbackLoggerMessage.Define(IntegrationLogEvents.ErrorProducingOutboxStoredMessage);

    private static readonly Action<ILogger, Exception?> ErrorProcessingOutbox =
        SilverbackLoggerMessage.Define(IntegrationLogEvents.ErrorProcessingOutbox);

    private static readonly Action<ILogger, string, Exception?> InvalidEndpointConfiguration =
        SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.InvalidEndpointConfiguration);

    private static readonly Action<ILogger, string, Exception?> EndpointConfiguratorError =
        SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.EndpointConfiguratorError);

    private static readonly Action<ILogger, Exception?> CallbackError =
        SilverbackLoggerMessage.Define(IntegrationLogEvents.CallbackError);

    private static readonly Action<ILogger, string?, Exception?> EndpointBuilderError =
        SilverbackLoggerMessage.Define<string?>(IntegrationLogEvents.EndpointBuilderError);

    public static void LogConsumerFatalError(this ISilverbackLogger logger, IConsumer consumer, Exception exception) =>
        ConsumerFatalError(logger.InnerLogger, consumer.DisplayName, exception);

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

    public static void LogReadingMessagesFromOutbox(this ISilverbackLogger logger, int packageSize) =>
        ReadingMessagesFromOutbox(logger.InnerLogger, packageSize, null);

    public static void LogOutboxEmpty(this ISilverbackLogger logger) =>
        OutboxEmpty(logger.InnerLogger, null);

    public static void LogProcessingOutboxStoredMessage(this ISilverbackLogger logger, int currentIndex) =>
        ProcessingOutboxStoredMessage(logger.InnerLogger, currentIndex, null);

    public static void LogErrorProducingOutboxStoredMessage(this ISilverbackLogger logger, Exception exception) =>
        ErrorProducingOutboxStoredMessage(logger.InnerLogger, exception);

    public static void LogErrorProcessingOutbox(this ISilverbackLogger logger, Exception exception) =>
        ErrorProcessingOutbox(logger.InnerLogger, exception);

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
    public static void LogLowLevelTrace(this ISilverbackLogger logger, string message, Func<object[]> argumentsProvider)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.LowLevelTracing))
            return;

        logger.InnerLogger.Log(
            IntegrationLogEvents.LowLevelTracing.Level,
            IntegrationLogEvents.LowLevelTracing.EventId,
            message,
            argumentsProvider.Invoke());
    }

    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem", Justification = "Optimized via IsEnabled")]
    [SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Optimized via IsEnabled")]
    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Optimized via IsEnabled")]
    public static void LogLowLevelTrace(
        this ISilverbackLogger logger,
        Exception? exception,
        string message,
        Func<object[]> argumentsProvider)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.LowLevelTracing))
            return;

        logger.InnerLogger.Log(
            IntegrationLogEvents.LowLevelTracing.Level,
            IntegrationLogEvents.LowLevelTracing.EventId,
            exception,
            message,
            argumentsProvider.Invoke());
    }

    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem", Justification = "Optimized via IsEnabled")]
    [SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Optimized via IsEnabled")]
    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Optimized via IsEnabled")]
    public static void LogConsumerLowLevelTrace(
        this ISilverbackLogger logger,
        IConsumer? consumer,
        string message,
        Func<object?[]>? argumentsProvider = null)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.LowLevelTracing))
            return;

        object?[] args =
        [
            ..argumentsProvider?.Invoke() ?? [],
            consumer?.DisplayName ?? string.Empty
        ];

        logger.InnerLogger.Log(
            IntegrationLogEvents.LowLevelTracing.Level,
            IntegrationLogEvents.LowLevelTracing.EventId,
            message + " | consumerName: {consumerName}",
            args);
    }
}
