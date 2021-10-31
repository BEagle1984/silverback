// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Mqtt;

namespace Silverback.Diagnostics
{
    internal static class MqttLoggerExtensions
    {
        private static readonly Action<ILogger, string, string, string, string, Exception?> ConsumingMessage =
            SilverbackLoggerMessage.Define<string, string, string, string>(
                IntegrationLoggerExtensions.EnrichConsumerLogEvent(MqttLogEvents.ConsumingMessage));

        private static readonly Action<ILogger, string, Exception?> ConnectError =
            SilverbackLoggerMessage.Define<string>(MqttLogEvents.ConnectError);

        private static readonly Action<ILogger, string, Exception?> ConnectRetryError =
            SilverbackLoggerMessage.Define<string>(MqttLogEvents.ConnectRetryError);

        private static readonly Action<ILogger, string, Exception?>
            ConnectionLost =
                SilverbackLoggerMessage.Define<string>(MqttLogEvents.ConnectionLost);

        private static readonly Action<ILogger, string, string, Exception?> ProducerQueueProcessingCanceled =
            SilverbackLoggerMessage.Define<string, string>(
                IntegrationLoggerExtensions.EnrichProducerLogEvent(
                    MqttLogEvents.ProducerQueueProcessingCanceled));

        private static readonly Action<ILogger, string, string, Exception?> MqttClientLogError =
            SilverbackLoggerMessage.Define<string, string>(MqttLogEvents.MqttClientLogError);

        private static readonly Action<ILogger, string, string, Exception?> MqttClientLogWarning =
            SilverbackLoggerMessage.Define<string, string>(MqttLogEvents.MqttClientLogWarning);

        private static readonly Action<ILogger, string, string, Exception?> MqttClientLogInformation =
            SilverbackLoggerMessage.Define<string, string>(MqttLogEvents.MqttClientLogInformation);

        private static readonly Action<ILogger, string, string, Exception?> MqttClientLogVerbose =
            SilverbackLoggerMessage.Define<string, string>(MqttLogEvents.MqttClientLogVerbose);

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

        public static void LogMqttClientError(
            this ISilverbackLogger logger,
            string? source,
            string message,
            object[]? parameters,
            Exception? exception)
        {
            if (!logger.IsEnabled(MqttLogEvents.MqttClientLogError))
                return;

            MqttClientLogError(
                logger.InnerLogger,
                source ?? "-",
                FormatMqttClientMessage(message, parameters),
                exception);
        }

        public static void LogMqttClientWarning(
            this ISilverbackLogger logger,
            string? source,
            string message,
            object[]? parameters,
            Exception? exception)
        {
            if (!logger.IsEnabled(MqttLogEvents.MqttClientLogWarning))
                return;

            MqttClientLogWarning(
                logger.InnerLogger,
                source ?? "-",
                FormatMqttClientMessage(message, parameters),
                exception);
        }

        public static void LogMqttClientInformation(
            this ISilverbackLogger logger,
            string? source,
            string message,
            object[]? parameters,
            Exception? exception)
        {
            if (!logger.IsEnabled(MqttLogEvents.MqttClientLogInformation))
                return;

            MqttClientLogInformation(
                logger.InnerLogger,
                source ?? "-",
                FormatMqttClientMessage(message, parameters),
                exception);
        }

        public static void LogMqttClientVerbose(
            this ISilverbackLogger logger,
            string? source,
            string message,
            object[]? parameters,
            Exception? exception)
        {
            if (!logger.IsEnabled(MqttLogEvents.MqttClientLogVerbose))
                return;

            MqttClientLogVerbose(
                logger.InnerLogger,
                source ?? "-",
                FormatMqttClientMessage(message, parameters),
                exception);
        }

        [SuppressMessage("", "CA1031", Justification = "Can't do anything but swallow all exceptions")]
        private static string FormatMqttClientMessage(string message, object[]? parameters)
        {
            try
            {
                return parameters != null && parameters.Length > 0
                    ? string.Format(CultureInfo.InvariantCulture, message, parameters)
                    : message;
            }
            catch
            {
                return message;
            }
        }
    }
}
