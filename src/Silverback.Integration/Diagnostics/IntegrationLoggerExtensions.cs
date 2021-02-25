// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;

namespace Silverback.Diagnostics
{
    internal static class IntegrationLoggerExtensions
    {
        private const string ConsumerEventsIdDataString =
            " | consumerId: {consumerId}";

        private const string ConsumerEventsEndpointDataString =
            ", endpointName: {endpointName}";

        private const string ConsumerEventsIdentifiersDataString =
            ", identifiers: {identifiers}";

        private const string ProducerEventsDataString =
            " | producerId: {producerId}, endpointName: {endpointName}";

        private static readonly Action<ILogger, string?, string, string, int, Exception?>
            MessageAddedToSequence = SilverbackLoggerMessage.Define<string?, string, string, int>(
                IntegrationLogEvents.MessageAddedToSequence);

        private static readonly Action<ILogger, string, string, Exception?>
            SequenceStarted = SilverbackLoggerMessage.Define<string, string>(
                IntegrationLogEvents.SequenceStarted);

        private static readonly Action<ILogger, string, string, int, Exception?>
            SequenceCompleted = SilverbackLoggerMessage.Define<string, string, int>(
                IntegrationLogEvents.SequenceCompleted);

        private static readonly Action<ILogger, string, string, int, SequenceAbortReason, Exception?>
            SequenceProcessingAborted =
                SilverbackLoggerMessage.Define<string, string, int, SequenceAbortReason>(
                    IntegrationLogEvents.SequenceProcessingAborted);

        private static readonly Action<ILogger, string, string, int, Exception?>
            ErrorProcessingInboundSequence =
                SilverbackLoggerMessage.Define<string, string, int>(
                    IntegrationLogEvents.ErrorProcessingInboundSequence);

        private static readonly Action<ILogger, string, string, int, Exception?>
            IncompleteSequenceAborted =
                SilverbackLoggerMessage.Define<string, string, int>(
                    IntegrationLogEvents.IncompleteSequenceAborted);

        private static readonly Action<ILogger, string, Exception?>
            SkippingIncompleteSequence =
                SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.SkippingIncompleteSequence);

        private static readonly Action<ILogger, string, string, Exception?>
            ErrorAbortingInboundSequence =
                SilverbackLoggerMessage.Define<string, string>(
                    IntegrationLogEvents.ErrorAbortingInboundSequence);

        private static readonly Action<ILogger, string, Exception?>
            BrokerConnecting =
                SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.BrokerConnecting);

        private static readonly Action<ILogger, string, Exception?>
            BrokerConnected =
                SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.BrokerConnected);

        private static readonly Action<ILogger, string, Exception?>
            BrokerDisconnecting =
                SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.BrokerDisconnecting);

        private static readonly Action<ILogger, string, Exception?>
            BrokerDisconnected =
                SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.BrokerDisconnected);

        private static readonly Action<ILogger, string, Exception?>
            CreatingNewConsumer =
                SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.CreatingNewConsumer);

        private static readonly Action<ILogger, string, Exception?>
            CreatingNewProducer =
                SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.CreatingNewProducer);

        private static readonly Action<ILogger, Exception?>
            BrokerConnectionError =
                SilverbackLoggerMessage.Define(IntegrationLogEvents.BrokerConnectionError);

        private static readonly Action<ILogger, string, string, Exception?>
            ConsumerConnected =
                SilverbackLoggerMessage.Define<string, string>(
                    EnrichConsumerLogEvent(IntegrationLogEvents.ConsumerConnected));

        private static readonly Action<ILogger, string, string, Exception?>
            ConsumerDisconnected =
                SilverbackLoggerMessage.Define<string, string>(
                    EnrichConsumerLogEvent(IntegrationLogEvents.ConsumerDisconnected));

        private static readonly Action<ILogger, string, string, Exception?>
            ConsumerFatalError =
                SilverbackLoggerMessage.Define<string, string>(
                    EnrichConsumerLogEvent(IntegrationLogEvents.ConsumerFatalError));

        private static readonly Action<ILogger, string, string, Exception?>
            ConsumerDisposingError =
                SilverbackLoggerMessage.Define<string, string>(
                    EnrichConsumerLogEvent(IntegrationLogEvents.ConsumerDisposingError));

        private static readonly Action<ILogger, string, string, string, Exception?>
            ConsumerCommitError =
                SilverbackLoggerMessage.Define<string, string, string>(
                    EnrichConsumerLogEvent(IntegrationLogEvents.ConsumerCommitError, addIdentifiers: true));

        private static readonly Action<ILogger, string, string, string, Exception?>
            ConsumerRollbackError =
                SilverbackLoggerMessage.Define<string, string, string>(
                    EnrichConsumerLogEvent(IntegrationLogEvents.ConsumerRollbackError, addIdentifiers: true));

        private static readonly Action<ILogger, string, string, Exception?>
            ConsumerConnectError =
                SilverbackLoggerMessage.Define<string, string>(
                    EnrichConsumerLogEvent(IntegrationLogEvents.ConsumerConnectError));

        private static readonly Action<ILogger, string, string, Exception?>
            ConsumerDisconnectError =
                SilverbackLoggerMessage.Define<string, string>(
                    EnrichConsumerLogEvent(IntegrationLogEvents.ConsumerDisconnectError));

        private static readonly Action<ILogger, string, string, Exception?>
            ConsumerStartError =
                SilverbackLoggerMessage.Define<string, string>(
                    EnrichConsumerLogEvent(IntegrationLogEvents.ConsumerStartError));

        private static readonly Action<ILogger, string, string, Exception?>
            ConsumerStopError =
                SilverbackLoggerMessage.Define<string, string>(
                    EnrichConsumerLogEvent(IntegrationLogEvents.ConsumerStopError));

        private static readonly Action<ILogger, string, string, Exception?>
            ProducerConnected =
                SilverbackLoggerMessage.Define<string, string>(
                    EnrichProducerLogEvent(IntegrationLogEvents.ProducerConnected));

        private static readonly Action<ILogger, string, string, Exception?>
            ProducerDisconnected =
                SilverbackLoggerMessage.Define<string, string>(
                    EnrichProducerLogEvent(IntegrationLogEvents.ProducerDisconnected));

        private static readonly Action<ILogger, int, Exception?> ReadingMessagesFromOutbox =
            SilverbackLoggerMessage.Define<int>(IntegrationLogEvents.ReadingMessagesFromOutbox);

        private static readonly Action<ILogger, Exception?> OutboxEmpty =
            SilverbackLoggerMessage.Define(IntegrationLogEvents.OutboxEmpty);

        private static readonly Action<ILogger, int, int, Exception?> ProcessingOutboxStoredMessage =
            SilverbackLoggerMessage.Define<int, int>(IntegrationLogEvents.ProcessingOutboxStoredMessage);

        private static readonly Action<ILogger, Exception?> ErrorProcessingOutbox =
            SilverbackLoggerMessage.Define(IntegrationLogEvents.ErrorProcessingOutbox);

        private static readonly Action<ILogger, string, Exception?> InvalidEndpointConfiguration =
            SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.InvalidEndpointConfiguration);

        private static readonly Action<ILogger, string, Exception?> EndpointConfiguratorError =
            SilverbackLoggerMessage.Define<string>(IntegrationLogEvents.EndpointConfiguratorError);

        private static readonly Action<ILogger, Exception?> CallbackHandlerError =
            SilverbackLoggerMessage.Define(IntegrationLogEvents.CallbackHandlerError);

        public static void LogMessageAddedToSequence(
            this ISilverbackLogger logger,
            IRawInboundEnvelope envelope,
            ISequence sequence)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.MessageAddedToSequence))
                return;

            MessageAddedToSequence(
                logger.InnerLogger,
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
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

        public static void LogSequenceProcessingError(
            this ISilverbackLogger logger,
            ISequence sequence,
            Exception exception)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.ErrorProcessingInboundSequence))
                return;

            ErrorProcessingInboundSequence(
                logger.InnerLogger,
                sequence.GetType().Name,
                sequence.SequenceId,
                sequence.Length,
                exception);
        }

        public static void LogIncompleteSequenceAborted(
            this ISilverbackLogger logger,
            ISequence sequence)
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

        public static void LogSkippingIncompleteSequence(
            this ISilverbackLogger logger,
            IncompleteSequence sequence) =>
            SkippingIncompleteSequence(
                logger.InnerLogger,
                sequence.SequenceId,
                null);

        public static void LogSequenceAbortingError(
            this ISilverbackLogger logger,
            ISequence sequence,
            Exception exception)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.ErrorAbortingInboundSequence))
                return;

            ErrorAbortingInboundSequence(
                logger.InnerLogger,
                sequence.GetType().Name,
                sequence.SequenceId,
                exception);
        }

        public static void LogBrokerConnecting(this ISilverbackLogger logger, IBroker broker) =>
            BrokerConnecting(logger.InnerLogger, broker.GetType().Name, null);

        public static void LogBrokerConnected(this ISilverbackLogger logger, IBroker broker) =>
            BrokerConnected(logger.InnerLogger, broker.GetType().Name, null);

        public static void LogBrokerDisconnecting(this ISilverbackLogger logger, IBroker broker) =>
            BrokerDisconnecting(logger.InnerLogger, broker.GetType().Name, null);

        public static void LogBrokerDisconnected(this ISilverbackLogger logger, IBroker broker) =>
            BrokerDisconnected(logger.InnerLogger, broker.GetType().Name, null);

        public static void LogCreatingNewConsumer(
            this ISilverbackLogger logger,
            IConsumerEndpoint endpoint) =>
            CreatingNewConsumer(logger.InnerLogger, endpoint.Name, null);

        public static void LogCreatingNewProducer(
            this ISilverbackLogger logger,
            IProducerEndpoint endpoint) =>
            CreatingNewProducer(logger.InnerLogger, endpoint.Name, null);

        public static void LogBrokerConnectionError(
            this ISilverbackLogger logger,
            Exception exception) =>
            BrokerConnectionError(logger.InnerLogger, exception);

        public static void LogConsumerConnected(
            this ISilverbackLogger logger,
            IConsumer consumer) =>
            ConsumerConnected(logger.InnerLogger, consumer.Id, consumer.Endpoint.Name, null);

        public static void LogConsumerDisconnected(
            this ISilverbackLogger logger,
            IConsumer consumer) =>
            ConsumerDisconnected(logger.InnerLogger, consumer.Id, consumer.Endpoint.Name, null);

        public static void LogConsumerFatalError(
            this ISilverbackLogger logger,
            IConsumer? consumer,
            Exception exception) =>
            ConsumerFatalError(
                logger.InnerLogger,
                consumer?.Id ?? "?",
                consumer?.Endpoint.Name ?? "?",
                exception);

        public static void LogConsumerDisposingError(
            this ISilverbackLogger logger,
            IConsumer consumer,
            Exception exception) =>
            ConsumerDisposingError(logger.InnerLogger, consumer.Id, consumer.Endpoint.Name, exception);

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
                consumer.Id,
                consumer.Endpoint.Name,
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
                consumer.Id,
                consumer.Endpoint.Name,
                string.Join(", ", identifiers.Select(identifier => identifier.ToVerboseLogString())),
                exception);
        }

        public static void LogConsumerConnectError(
            this ISilverbackLogger logger,
            IConsumer consumer,
            Exception exception) =>
            ConsumerConnectError(logger.InnerLogger, consumer.Id, consumer.Endpoint.Name, exception);

        public static void LogConsumerDisconnectError(
            this ISilverbackLogger logger,
            IConsumer consumer,
            Exception exception) =>
            ConsumerDisconnectError(logger.InnerLogger, consumer.Id, consumer.Endpoint.Name, exception);

        public static void LogConsumerStartError(
            this ISilverbackLogger logger,
            IConsumer consumer,
            Exception exception) =>
            ConsumerStartError(logger.InnerLogger, consumer.Id, consumer.Endpoint.Name, exception);

        public static void LogConsumerStopError(
            this ISilverbackLogger logger,
            IConsumer consumer,
            Exception exception) =>
            ConsumerStopError(logger.InnerLogger, consumer.Id, consumer.Endpoint.Name, exception);

        public static void LogProducerConnected(
            this ISilverbackLogger logger,
            IProducer producer) =>
            ProducerConnected(logger.InnerLogger, producer.Id, producer.Endpoint.Name, null);

        public static void LogProducerDisconnected(
            this ISilverbackLogger logger,
            IProducer producer) =>
            ProducerDisconnected(logger.InnerLogger, producer.Id, producer.Endpoint.Name, null);

        public static void LogReadingMessagesFromOutbox(this ISilverbackLogger logger, int packageSize) =>
            ReadingMessagesFromOutbox(logger.InnerLogger, packageSize, null);

        public static void LogOutboxEmpty(this ISilverbackLogger logger) =>
            OutboxEmpty(logger.InnerLogger, null);

        public static void LogProcessingOutboxStoredMessage(
            this ISilverbackLogger logger,
            int currentIndex,
            int count) =>
            ProcessingOutboxStoredMessage(logger.InnerLogger, currentIndex, count, null);

        public static void LogErrorProcessingOutbox(
            this ISilverbackLogger logger,
            Exception exception) =>
            ErrorProcessingOutbox(logger.InnerLogger, exception);

        public static void LogInvalidEndpointConfiguration(
            this ISilverbackLogger logger,
            IEndpoint endpoint,
            Exception exception) =>
            InvalidEndpointConfiguration(logger.InnerLogger, endpoint.Name, exception);

        public static void LogEndpointConfiguratorError(
            this ISilverbackLogger logger,
            IEndpointsConfigurator configurator,
            Exception exception) =>
            EndpointConfiguratorError(logger.InnerLogger, configurator.GetType().Name, exception);

        public static void LogCallbackHandlerError(
            this ISilverbackLogger logger,
            Exception exception) =>
            CallbackHandlerError(logger.InnerLogger, exception);

        public static void LogLowLevelTrace(
            this ISilverbackLogger logger,
            string message,
            Func<object[]> argumentsProvider)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.LowLevelTracing))
                return;

            logger.InnerLogger.Log(
                IntegrationLogEvents.LowLevelTracing.Level,
                IntegrationLogEvents.LowLevelTracing.EventId,
                message,
                argumentsProvider.Invoke());
        }

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

        public static void LogConsumerLowLevelTrace(
            this ISilverbackLogger logger,
            IConsumer? consumer,
            string message,
            Func<object?[]>? argumentsProvider = null)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.LowLevelTracing))
                return;

            var args = new List<object?>(argumentsProvider?.Invoke() ?? Array.Empty<object>());
            args.Add(consumer?.Id ?? string.Empty);
            args.Add(consumer?.Endpoint.Name ?? string.Empty);

            logger.InnerLogger.Log(
                IntegrationLogEvents.LowLevelTracing.Level,
                IntegrationLogEvents.LowLevelTracing.EventId,
                message + GetConsumerMessageDataString(),
                args.ToArray());
        }

        public static LogEvent EnrichConsumerLogEvent(
            LogEvent logEvent,
            bool addEndpointName = true,
            bool addIdentifiers = false) =>
            new(
                logEvent.Level,
                logEvent.EventId,
                logEvent.Message + GetConsumerMessageDataString(addEndpointName, addIdentifiers));

        public static LogEvent EnrichProducerLogEvent(LogEvent logEvent) =>
            new(logEvent.Level, logEvent.EventId, logEvent.Message + ProducerEventsDataString);

        private static string GetConsumerMessageDataString(
            bool addEndpointName = true,
            bool addIdentifiers = false)
        {
            var message = ConsumerEventsIdDataString;

            if (addEndpointName)
                message += ConsumerEventsEndpointDataString;

            if (addIdentifiers)
                message += ConsumerEventsIdentifiersDataString;

            return message;
        }
    }
}
