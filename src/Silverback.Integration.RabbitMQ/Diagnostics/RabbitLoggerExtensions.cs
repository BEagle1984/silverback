// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;

namespace Silverback.Diagnostics
{
    internal static class RabbitLoggerExtensions
    {
        private static readonly Action<ILogger, string, string, string, Exception?>
            ConsumingMessage =
                SilverbackLoggerMessage.Define<string, string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(RabbitLogEvents.ConsumingMessage));

        private static readonly Action<ILogger, string, string, string, Exception?>
            Commit =
                SilverbackLoggerMessage.Define<string, string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(RabbitLogEvents.Commit));

        private static readonly Action<ILogger, string, string, string, Exception?>
            Rollback =
                SilverbackLoggerMessage.Define<string, string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(RabbitLogEvents.Rollback));

        private static readonly Action<ILogger, string, string, Exception?>
            ProducerQueueProcessingCanceled =
                SilverbackLoggerMessage.Define<string, string>(
                    IntegrationLoggerExtensions.EnrichProducerLogEvent(
                        RabbitLogEvents.ProducerQueueProcessingCanceled));

        public static void LogConsuming(
            this ISilverbackLogger logger,
            string deliveryTag,
            RabbitConsumer consumer) =>
            ConsumingMessage(
                logger.InnerLogger,
                deliveryTag,
                consumer.Id,
                consumer.Endpoint.DisplayName,
                null);

        public static void LogCommit(
            this ISilverbackLogger logger,
            string deliveryTag,
            RabbitConsumer consumer) =>
            Commit(logger.InnerLogger, deliveryTag, consumer.Id, consumer.Endpoint.DisplayName, null);

        public static void LogRollback(
            this ISilverbackLogger logger,
            string deliveryTag,
            RabbitConsumer consumer) =>
            Rollback(logger.InnerLogger, deliveryTag, consumer.Id, consumer.Endpoint.DisplayName, null);

        public static void LogProducerQueueProcessingCanceled(
            this ISilverbackLogger logger,
            RabbitProducer producer) =>
            ProducerQueueProcessingCanceled(
                logger.InnerLogger,
                producer.Id,
                producer.Endpoint.DisplayName,
                null);
    }
}
