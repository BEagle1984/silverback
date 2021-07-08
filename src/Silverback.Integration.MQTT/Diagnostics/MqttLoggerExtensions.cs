// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Mqtt;

namespace Silverback.Diagnostics
{
    internal static class MqttLoggerExtensions
    {
        private static readonly Action<ILogger, string, string, string, string, Exception?>
            ConsumingMessage =
                SilverbackLoggerMessage.Define<string, string, string, string>(
                    IntegrationLoggerExtensions.EnrichConsumerLogEvent(MqttLogEvents.ConsumingMessage));

        private static readonly Action<ILogger, string, Exception?>
            ConnectError =
                SilverbackLoggerMessage.Define<string>(MqttLogEvents.ConnectError);

        private static readonly Action<ILogger, string, Exception?>
            ConnectRetryError =
                SilverbackLoggerMessage.Define<string>(MqttLogEvents.ConnectRetryError);

        private static readonly Action<ILogger, string, Exception?>
            ConnectionLost =
                SilverbackLoggerMessage.Define<string>(MqttLogEvents.ConnectionLost);

        private static readonly Action<ILogger, string, string, Exception?>
            ProducerQueueProcessingCanceled =
                SilverbackLoggerMessage.Define<string, string>(
                    IntegrationLoggerExtensions.EnrichProducerLogEvent(
                        MqttLogEvents.ProducerQueueProcessingCanceled));

        public static void LogConsuming(
            this ISilverbackLogger logger,
            ConsumedApplicationMessage applicationMessage,
            MqttConsumer consumer) =>
            ConsumingMessage(
                logger.InnerLogger,
                applicationMessage.Id,
                applicationMessage.ApplicationMessage.Topic,
                consumer.Id,
                applicationMessage.ApplicationMessage.Topic,
                null);

        public static void LogConnectError(
            this ISilverbackLogger logger,
            MqttClientWrapper client,
            Exception exception) =>
            ConnectError(
                logger.InnerLogger,
                client.ClientConfig.ClientId,
                exception);

        public static void LogConnectRetryError(
            this ISilverbackLogger logger,
            MqttClientWrapper client,
            Exception exception) =>
            ConnectRetryError(
                logger.InnerLogger,
                client.ClientConfig.ClientId,
                exception);

        public static void LogConnectionLost(
            this ISilverbackLogger logger,
            MqttClientWrapper client) =>
            ConnectionLost(
                logger.InnerLogger,
                client.ClientConfig.ClientId,
                null);

        public static void LogProducerQueueProcessingCanceled(
            this ISilverbackLogger logger,
            MqttProducer producer) =>
            ProducerQueueProcessingCanceled(
                logger.InnerLogger,
                producer.Id,
                producer.Endpoint.DisplayName,
                null);
    }
}
