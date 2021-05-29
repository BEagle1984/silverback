// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;

namespace Silverback.Diagnostics
{
    internal static class KafkaLoggerExtensions
    {
        private static readonly Action<ILogger, string, int, long, string, string, Exception?>
            ConsumingMessage =
                SilverbackLoggerMessage.Define<string, int, long, string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(KafkaLogEvents.ConsumingMessage));

        private static readonly Action<ILogger, string, int, long, string, string, Exception?>
            EndOfPartition =
                SilverbackLoggerMessage.Define<string, int, long, string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(KafkaLogEvents.EndOfPartition));

        private static readonly Action<ILogger, string, string, Exception?>
            KafkaExceptionAutoRecovery =
                SilverbackLoggerMessage.Define<string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.KafkaExceptionAutoRecovery));

        private static readonly Action<ILogger, string, string, Exception?>
            KafkaExceptionNoAutoRecovery =
                SilverbackLoggerMessage.Define<string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.KafkaExceptionNoAutoRecovery));

        private static readonly Action<ILogger, string, string, Exception?>
            ConsumingCanceled =
                SilverbackLoggerMessage.Define<string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(KafkaLogEvents.ConsumingCanceled));

        private static readonly Action<ILogger, string, string, Exception?>
            CreatingConfluentProducer =
                SilverbackLoggerMessage.Define<string, string>(
                    IntegrationLoggerExtensions.EnrichProducerLogEvent(
                        KafkaLogEvents.CreatingConfluentProducer));

        private static readonly Action<ILogger, string, string, Exception?>
            ProduceNotAcknowledged =
                SilverbackLoggerMessage.Define<string, string>(
                    IntegrationLoggerExtensions.EnrichProducerLogEvent(
                        KafkaLogEvents.ProduceNotAcknowledged));

        private static readonly Action<ILogger, string, int, string, Exception?>
            PartitionAssigned =
                SilverbackLoggerMessage.Define<string, int, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.PartitionAssigned,
                        false));

        private static readonly Action<ILogger, string, int, long, string, Exception?>
            PartitionOffsetReset =
                SilverbackLoggerMessage.Define<string, int, long, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.PartitionOffsetReset,
                        false));

        private static readonly Action<ILogger, string, int, long, string, Exception?>
            PartitionRevoked =
                SilverbackLoggerMessage.Define<string, int, long, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.PartitionRevoked,
                        false));

        private static readonly Action<ILogger, string, int, long, string, Exception?>
            OffsetCommitted =
                SilverbackLoggerMessage.Define<string, int, long, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.OffsetCommitted,
                        false));

        private static readonly Action<ILogger, string, int, long, string, int, string, Exception?>
            OffsetCommitError =
                SilverbackLoggerMessage.Define<string, int, long, string, int, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.OffsetCommitError,
                        false));

        private static readonly Action<ILogger, string, int, string, string, Exception?>
            ConfluentConsumerFatalError =
                SilverbackLoggerMessage.Define<string, int, string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.ConfluentConsumerFatalError));

        private static readonly Action<ILogger, string, string, Exception?>
            KafkaConsumerErrorHandlerError =
                SilverbackLoggerMessage.Define<string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.KafkaErrorHandlerError));

        private static readonly Action<ILogger, string, string, Exception?>
            KafkaConsumerLogHandlerError =
                SilverbackLoggerMessage.Define<string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(KafkaLogEvents.KafkaLogHandlerError));

        private static readonly Action<ILogger, string, string, Exception?>
            KafkaProducerLogHandlerError =
                SilverbackLoggerMessage.Define<string, string>(
                    IntegrationLoggerExtensions.EnrichProducerLogEvent(KafkaLogEvents.KafkaLogHandlerError));

        private static readonly Action<ILogger, string, string, string, Exception?>
            ConsumerStatisticsReceived =
                SilverbackLoggerMessage.Define<string, string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.ConsumerStatisticsReceived));

        private static readonly Action<ILogger, string, string, string, Exception?>
            ProducerStatisticsReceived =
                SilverbackLoggerMessage.Define<string, string, string>(
                    IntegrationLoggerExtensions.EnrichProducerLogEvent(
                        KafkaLogEvents.ProducerStatisticsReceived));

        private static readonly Action<ILogger, Exception?>
            StatisticsDeserializationError =
                SilverbackLoggerMessage.Define(KafkaLogEvents.StatisticsDeserializationError);

        private static readonly Action<ILogger, string, int, long, string, Exception?>
            PartitionManuallyAssigned =
                SilverbackLoggerMessage.Define<string, int, long, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.PartitionManuallyAssigned,
                        false));

        private static readonly Action<ILogger, string, int, string, string, Exception?>
            ConfluentConsumerError =
                SilverbackLoggerMessage.Define<string, int, string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.ConfluentConsumerError));

        private static readonly Action<ILogger, string, string, Exception?>
            ConfluentConsumerDisconnectError =
                SilverbackLoggerMessage.Define<string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.ConfluentConsumerDisconnectError));

        private static readonly Action<ILogger, string, string, string, string, Exception?>
            PollTimeoutAutoRecovery =
                SilverbackLoggerMessage.Define<string, string, string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.PollTimeoutAutoRecovery));

        private static readonly Action<ILogger, string, string, string, string, Exception?>
            PollTimeoutNoAutoRecovery =
                SilverbackLoggerMessage.Define<string, string, string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.PollTimeoutNoAutoRecovery));

        private static readonly Action<ILogger, string, string, string, string, Exception?>
            ConfluentProducerLogCritical =
                SilverbackLoggerMessage.Define<string, string, string, string>(
                    IntegrationLoggerExtensions.EnrichProducerLogEvent(
                        KafkaLogEvents.ConfluentProducerLogCritical));

        private static readonly Action<ILogger, string, string, string, string, Exception?>
            ConfluentProducerLogError =
                SilverbackLoggerMessage.Define<string, string, string, string>(
                    IntegrationLoggerExtensions.EnrichProducerLogEvent(
                        KafkaLogEvents.ConfluentProducerLogError));

        private static readonly Action<ILogger, string, string, string, string, Exception?>
            ConfluentProducerLogWarning =
                SilverbackLoggerMessage.Define<string, string, string, string>(
                    IntegrationLoggerExtensions.EnrichProducerLogEvent(
                        KafkaLogEvents.ConfluentProducerLogWarning));

        private static readonly Action<ILogger, string, string, string, string, Exception?>
            ConfluentProducerLogInformation =
                SilverbackLoggerMessage.Define<string, string, string, string>(
                    IntegrationLoggerExtensions.EnrichProducerLogEvent(
                        KafkaLogEvents.ConfluentProducerLogInformation));

        private static readonly Action<ILogger, string, string, string, string, Exception?>
            ConfluentProducerLogDebug =
                SilverbackLoggerMessage.Define<string, string, string, string>(
                    IntegrationLoggerExtensions.EnrichProducerLogEvent(
                        KafkaLogEvents.ConfluentProducerLogDebug));

        private static readonly Action<ILogger, string, string, string, string, Exception?>
            ConfluentConsumerLogCritical =
                SilverbackLoggerMessage.Define<string, string, string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.ConfluentConsumerLogCritical));

        private static readonly Action<ILogger, string, string, string, string, Exception?>
            ConfluentConsumerLogError =
                SilverbackLoggerMessage.Define<string, string, string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.ConfluentConsumerLogError));

        private static readonly Action<ILogger, string, string, string, string, Exception?>
            ConfluentConsumerLogWarning =
                SilverbackLoggerMessage.Define<string, string, string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.ConfluentConsumerLogWarning));

        private static readonly Action<ILogger, string, string, string, string, Exception?>
            ConfluentConsumerLogInformation =
                SilverbackLoggerMessage.Define<string, string, string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.ConfluentConsumerLogInformation));

        private static readonly Action<ILogger, string, string, string, string, Exception?>
            ConfluentConsumerLogDebug =
                SilverbackLoggerMessage.Define<string, string, string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(
                        KafkaLogEvents.ConfluentConsumerLogDebug));

        public static void LogConsuming(
            this ISilverbackLogger logger,
            ConsumeResult<byte[]?, byte[]?> consumeResult,
            KafkaConsumer consumer) =>
            ConsumingMessage(
                logger.InnerLogger,
                consumeResult.Topic,
                consumeResult.Partition,
                consumeResult.Offset,
                consumer.Id,
                consumeResult.Topic,
                null);

        public static void LogEndOfPartition(
            this ISilverbackLogger logger,
            ConsumeResult<byte[]?, byte[]?> consumeResult,
            KafkaConsumer consumer) =>
            EndOfPartition(
                logger.InnerLogger,
                consumeResult.Topic,
                consumeResult.Partition,
                consumeResult.Offset,
                consumer.Id,
                consumeResult.Topic,
                null);

        public static void LogKafkaExceptionAutoRecovery(
            this ISilverbackLogger logger,
            KafkaConsumer consumer,
            Exception exception) =>
            KafkaExceptionAutoRecovery(
                logger.InnerLogger,
                consumer.Id,
                consumer.Endpoint.Name,
                exception);

        public static void LogKafkaExceptionNoAutoRecovery(
            this ISilverbackLogger logger,
            KafkaConsumer consumer,
            Exception exception) =>
            KafkaExceptionNoAutoRecovery(
                logger.InnerLogger,
                consumer.Id,
                consumer.Endpoint.Name,
                exception);

        public static void LogConsumingCanceled(
            this ISilverbackLogger logger,
            KafkaConsumer consumer) =>
            ConsumingCanceled(
                logger.InnerLogger,
                consumer.Id,
                consumer.Endpoint.Name,
                null);

        public static void LogCreatingConfluentProducer(
            this ISilverbackLogger logger,
            KafkaProducer producer) =>
            CreatingConfluentProducer(
                logger.InnerLogger,
                producer.Id,
                producer.Endpoint.Name,
                null);

        public static void LogProduceNotAcknowledged(
            this ISilverbackLogger logger,
            KafkaProducer producer) =>
            ProduceNotAcknowledged(
                logger.InnerLogger,
                producer.Id,
                producer.Endpoint.Name,
                null);

        public static void LogPartitionAssigned(
            this ISilverbackLogger logger,
            TopicPartition topicPartition,
            KafkaConsumer consumer) =>
            PartitionAssigned(
                logger.InnerLogger,
                topicPartition.Topic,
                topicPartition.Partition,
                consumer.Id,
                null);

        public static void LogPartitionOffsetReset(
            this ISilverbackLogger logger,
            TopicPartitionOffset topicPartitionOffset,
            KafkaConsumer consumer) =>
            PartitionOffsetReset(
                logger.InnerLogger,
                topicPartitionOffset.Topic,
                topicPartitionOffset.Partition,
                topicPartitionOffset.Offset,
                consumer.Id,
                null);

        public static void LogPartitionRevoked(
            this ISilverbackLogger logger,
            TopicPartitionOffset topicPartitionOffset,
            KafkaConsumer consumer) =>
            PartitionRevoked(
                logger.InnerLogger,
                topicPartitionOffset.Topic,
                topicPartitionOffset.Partition,
                topicPartitionOffset.Offset,
                consumer.Id,
                null);

        public static void LogOffsetCommitted(
            this ISilverbackLogger logger,
            TopicPartitionOffset topicPartitionOffset,
            KafkaConsumer consumer) =>
            OffsetCommitted(
                logger.InnerLogger,
                topicPartitionOffset.Topic,
                topicPartitionOffset.Partition,
                topicPartitionOffset.Offset,
                consumer.Id,
                null);

        public static void LogOffsetCommitError(
            this ISilverbackLogger logger,
            TopicPartitionOffsetError topicPartitionOffsetError,
            KafkaConsumer consumer)
        {
            if (!logger.IsEnabled(KafkaLogEvents.OffsetCommitError))
                return;

            OffsetCommitError(
                logger.InnerLogger,
                topicPartitionOffsetError.Topic,
                topicPartitionOffsetError.Partition,
                topicPartitionOffsetError.Offset,
                GetErrorReason(topicPartitionOffsetError.Error),
                (int)topicPartitionOffsetError.Error.Code,
                consumer.Id,
                null);
        }

        public static void LogConfluentConsumerFatalError(
            this ISilverbackLogger logger,
            Error error,
            KafkaConsumer consumer)
        {
            if (!logger.IsEnabled(KafkaLogEvents.ConfluentConsumerFatalError))
                return;

            ConfluentConsumerFatalError(
                logger.InnerLogger,
                GetErrorReason(error),
                (int)error.Code,
                consumer.Id,
                consumer.Endpoint.Name,
                null);
        }

        public static void LogKafkaErrorHandlerError(
            this ISilverbackLogger logger,
            KafkaConsumer consumer,
            Exception exception) =>
            KafkaConsumerErrorHandlerError(
                logger.InnerLogger,
                consumer.Id,
                consumer.Endpoint.Name,
                exception);

        public static void LogKafkaLogHandlerError(
            this ISilverbackLogger logger,
            KafkaConsumer consumer,
            Exception exception) =>
            KafkaConsumerLogHandlerError(
                logger.InnerLogger,
                consumer.Id,
                consumer.Endpoint.Name,
                exception);

        public static void LogKafkaLogHandlerError(
            this ISilverbackLogger logger,
            KafkaProducer producer,
            Exception exception) =>
            KafkaProducerLogHandlerError(
                logger.InnerLogger,
                producer.Id,
                producer.Endpoint.Name,
                exception);

        public static void LogConsumerStatisticsReceived(
            this ISilverbackLogger logger,
            string statisticsJson,
            KafkaConsumer consumer) =>
            ConsumerStatisticsReceived(
                logger.InnerLogger,
                statisticsJson,
                consumer.Id,
                consumer.Endpoint.Name,
                null);

        public static void LogProducerStatisticsReceived(
            this ISilverbackLogger logger,
            string statisticsJson,
            KafkaProducer producer) =>
            ProducerStatisticsReceived(
                logger.InnerLogger,
                statisticsJson,
                producer.Id,
                producer.Endpoint.Name,
                null);

        public static void LogStatisticsDeserializationError(
            this ISilverbackLogger logger,
            Exception exception) =>
            StatisticsDeserializationError(logger.InnerLogger, exception);

        public static void LogPartitionManuallyAssigned(
            this ISilverbackLogger logger,
            TopicPartitionOffset topicPartitionOffset,
            KafkaConsumer consumer) =>
            PartitionManuallyAssigned(
                logger.InnerLogger,
                topicPartitionOffset.Topic,
                topicPartitionOffset.Partition,
                topicPartitionOffset.Offset,
                consumer.Id,
                null);

        public static void LogConfluentConsumerError(
            this ISilverbackLogger logger,
            Error error,
            KafkaConsumer consumer)
        {
            if (!logger.IsEnabled(KafkaLogEvents.ConfluentConsumerError))
                return;

            ConfluentConsumerError(
                logger.InnerLogger,
                GetErrorReason(error),
                (int)error.Code,
                consumer.Id,
                consumer.Endpoint.Name,
                null);
        }

        public static void LogConfluentConsumerDisconnectError(
            this ISilverbackLogger logger,
            KafkaConsumer consumer,
            Exception exception) =>
            ConfluentConsumerDisconnectError(
                logger.InnerLogger,
                consumer.Id,
                consumer.Endpoint.Name,
                exception);

        public static void LogPollTimeoutAutoRecovery(
            this ISilverbackLogger logger,
            LogMessage logMessage,
            KafkaConsumer consumer) =>
            PollTimeoutAutoRecovery(
                logger.InnerLogger,
                logMessage.Level.ToString(),
                logMessage.Message,
                consumer.Id,
                consumer.Endpoint.Name,
                null);

        public static void LogPollTimeoutNoAutoRecovery(
            this ISilverbackLogger logger,
            LogMessage logMessage,
            KafkaConsumer consumer) =>
            PollTimeoutNoAutoRecovery(
                logger.InnerLogger,
                logMessage.Level.ToString(),
                logMessage.Message,
                consumer.Id,
                consumer.Endpoint.Name,
                null);

        public static void LogConfluentProducerLogCritical(
            this ISilverbackLogger logger,
            LogMessage logMessage,
            KafkaProducer producer) =>
            ConfluentProducerLogCritical(
                logger.InnerLogger,
                logMessage.Level.ToString(),
                logMessage.Message,
                producer.Id,
                producer.Endpoint.Name,
                null);

        public static void LogConfluentProducerLogError(
            this ISilverbackLogger logger,
            LogMessage logMessage,
            KafkaProducer producer) =>
            ConfluentProducerLogError(
                logger.InnerLogger,
                logMessage.Level.ToString(),
                logMessage.Message,
                producer.Id,
                producer.Endpoint.Name,
                null);

        public static void LogConfluentProducerLogWarning(
            this ISilverbackLogger logger,
            LogMessage logMessage,
            KafkaProducer producer) =>
            ConfluentProducerLogWarning(
                logger.InnerLogger,
                logMessage.Level.ToString(),
                logMessage.Message,
                producer.Id,
                producer.Endpoint.Name,
                null);

        public static void LogConfluentProducerLogInformation(
            this ISilverbackLogger logger,
            LogMessage logMessage,
            KafkaProducer producer) =>
            ConfluentProducerLogInformation(
                logger.InnerLogger,
                logMessage.Level.ToString(),
                logMessage.Message,
                producer.Id,
                producer.Endpoint.Name,
                null);

        public static void LogConfluentProducerLogDebug(
            this ISilverbackLogger logger,
            LogMessage logMessage,
            KafkaProducer producer) =>
            ConfluentProducerLogDebug(
                logger.InnerLogger,
                logMessage.Level.ToString(),
                logMessage.Message,
                producer.Id,
                producer.Endpoint.Name,
                null);

        public static void LogConfluentConsumerLogCritical(
            this ISilverbackLogger logger,
            LogMessage logMessage,
            KafkaConsumer consumer) =>
            ConfluentConsumerLogCritical(
                logger.InnerLogger,
                logMessage.Level.ToString(),
                logMessage.Message,
                consumer.Id,
                consumer.Endpoint.Name,
                null);

        public static void LogConfluentConsumerLogError(
            this ISilverbackLogger logger,
            LogMessage logMessage,
            KafkaConsumer consumer) =>
            ConfluentConsumerLogError(
                logger.InnerLogger,
                logMessage.Level.ToString(),
                logMessage.Message,
                consumer.Id,
                consumer.Endpoint.Name,
                null);

        public static void LogConfluentConsumerLogWarning(
            this ISilverbackLogger logger,
            LogMessage logMessage,
            KafkaConsumer consumer) =>
            ConfluentConsumerLogWarning(
                logger.InnerLogger,
                logMessage.Level.ToString(),
                logMessage.Message,
                consumer.Id,
                consumer.Endpoint.Name,
                null);

        public static void LogConfluentConsumerLogInformation(
            this ISilverbackLogger logger,
            LogMessage logMessage,
            KafkaConsumer consumer) =>
            ConfluentConsumerLogInformation(
                logger.InnerLogger,
                logMessage.Level.ToString(),
                logMessage.Message,
                consumer.Id,
                consumer.Endpoint.Name,
                null);

        public static void LogConfluentConsumerLogDebug(
            this ISilverbackLogger logger,
            LogMessage logMessage,
            KafkaConsumer consumer) =>
            ConfluentConsumerLogDebug(
                logger.InnerLogger,
                logMessage.Level.ToString(),
                logMessage.Message,
                consumer.Id,
                consumer.Endpoint.Name,
                null);

        private static string GetErrorReason(Error error) =>
            !string.IsNullOrEmpty(error.Reason) ? error.Reason : error.Code.GetReason();
    }
}
