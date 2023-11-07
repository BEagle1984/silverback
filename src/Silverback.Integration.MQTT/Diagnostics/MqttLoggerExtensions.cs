// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Mqtt;

namespace Silverback.Diagnostics;

// TODO: Move something to ConsumerLogger or ProducerLogger (enriched)?
internal static class MqttLoggerExtensions
{
    private static readonly Action<ILogger, string, string, string, Exception?> ConsumingMessage =
        SilverbackLoggerMessage.Define<string, string, string>(MqttLogEvents.ConsumingMessage);

    private static readonly Action<ILogger, string, string, string, Exception?> AcknowledgeFailed =
        SilverbackLoggerMessage.Define<string, string, string>(MqttLogEvents.AcknowledgeFailed);

    private static readonly Action<ILogger, string, string, string, Exception?> ConnectError =
        SilverbackLoggerMessage.Define<string, string, string>(MqttLogEvents.ConnectError);

    private static readonly Action<ILogger, string, string, string, Exception?> ConnectRetryError =
        SilverbackLoggerMessage.Define<string, string, string>(MqttLogEvents.ConnectRetryError);

    private static readonly Action<ILogger, string, string, string, Exception?> ConnectionLost =
        SilverbackLoggerMessage.Define<string, string, string>(MqttLogEvents.ConnectionLost);

    private static readonly Action<ILogger, string, string, string, Exception?> Reconnected =
        SilverbackLoggerMessage.Define<string, string, string>(MqttLogEvents.Reconnected);

    private static readonly Action<ILogger, string, string, Exception?> ProducerQueueProcessingCanceled =
        SilverbackLoggerMessage.Define<string, string>(MqttLogEvents.ProducerQueueProcessingCanceled);

    private static readonly Action<ILogger, string, string, string, string, Exception?> ConsumerSubscribed =
        SilverbackLoggerMessage.Define<string, string, string, string>(MqttLogEvents.ConsumerSubscribed);

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
            consumer.DisplayName,
            null);

    public static void LogAcknowledgeFailed(
        this ISilverbackLogger logger,
        ConsumedApplicationMessage applicationMessage,
        MqttConsumer consumer,
        Exception exception) =>
        AcknowledgeFailed(
            logger.InnerLogger,
            applicationMessage.Id,
            applicationMessage.ApplicationMessage.Topic,
            consumer.DisplayName,
            exception);

    public static void LogConnectError(
        this ISilverbackLogger logger,
        MqttClientWrapper client,
        Exception exception) =>
        ConnectError(
            logger.InnerLogger,
            client.DisplayName,
            client.Configuration.ClientId,
            client.Configuration.Channel?.ToString() ?? string.Empty,
            exception);

    public static void LogConnectRetryError(
        this ISilverbackLogger logger,
        MqttClientWrapper client,
        Exception exception) =>
        ConnectRetryError(
            logger.InnerLogger,
            client.DisplayName,
            client.Configuration.ClientId,
            client.Configuration.Channel?.ToString() ?? string.Empty,
            exception);

    public static void LogConnectionLost(this ISilverbackLogger logger, MqttClientWrapper client) =>
        ConnectionLost(
            logger.InnerLogger,
            client.DisplayName,
            client.Configuration.ClientId,
            client.Configuration.Channel?.ToString() ?? string.Empty,
            null);

    public static void LogReconnected(this ISilverbackLogger logger, MqttClientWrapper client) =>
        Reconnected(
            logger.InnerLogger,
            client.DisplayName,
            client.Configuration.ClientId,
            client.Configuration.Channel?.ToString() ?? string.Empty,
            null);

    public static void LogProducerQueueProcessingCanceled(this ISilverbackLogger logger, MqttClientWrapper client) =>
        ProducerQueueProcessingCanceled(logger.InnerLogger, client.DisplayName, client.Configuration.ClientId, null);

    public static void LogConsumerSubscribed(this ISilverbackLogger logger, string topicPattern, MqttConsumer consumer) =>
        ConsumerSubscribed(
            logger.InnerLogger,
            topicPattern,
            consumer.Client.DisplayName,
            consumer.Client.Configuration.ClientId,
            consumer.DisplayName,
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

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Can't do anything but swallow all exceptions")]
    private static string FormatMqttClientMessage(string message, object[]? parameters)
    {
        try
        {
            return parameters is { Length: > 0 }
                ? string.Format(CultureInfo.InvariantCulture, message, parameters)
                : message;
        }
        catch
        {
            return message;
        }
    }
}
