// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Logging;
using MQTTnet;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Mqtt;

namespace Silverback.Diagnostics;

// The MqttLogEvents properties are generated from the [LoggerMessage] methods, including their documentation
internal static partial class MqttLoggerExtensions
{
    public static void LogConsuming(
        this ISilverbackLogger logger,
        ConsumedApplicationMessage applicationMessage,
        MqttConsumer consumer) =>
        ConsumingMessage(
            logger.InnerLogger,
            applicationMessage.Id,
            applicationMessage.ApplicationMessage.Topic,
            consumer.DisplayName);

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

    public static void LogNoMatchingSubscribers(this ISilverbackLogger logger, string topic) =>
        NoMatchingSubscribers(logger.InnerLogger, topic);

    public static void LogBatchProcessingEnabled(this ISilverbackLogger logger, MqttConsumer consumer) =>
        BatchProcessingEnabled(
            logger.InnerLogger,
            consumer.Configuration.MaxBatchedMessagesCount,
            consumer.Configuration.BatchSize ?? 1,
            consumer.Configuration.MaxDegreeOfParallelism,
            consumer.DisplayName);

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
            client.Configuration.Channel?.ToString() ?? string.Empty);

    public static void LogReconnected(this ISilverbackLogger logger, MqttClientWrapper client) =>
        Reconnected(
            logger.InnerLogger,
            client.DisplayName,
            client.Configuration.ClientId,
            client.Configuration.Channel?.ToString() ?? string.Empty);

    public static void LogProducerQueueProcessingCanceled(this ISilverbackLogger logger, MqttClientWrapper client) =>
        ProducerQueueProcessingCanceled(logger.InnerLogger, client.DisplayName, client.Configuration.ClientId);

    public static void LogConsumerSubscribed(this ISilverbackLogger logger, string topicPattern, MqttConsumer consumer) =>
        ConsumerSubscribed(
            logger.InnerLogger,
            topicPattern,
            consumer.Client.DisplayName,
            consumer.Client.Configuration.ClientId,
            consumer.DisplayName);

    public static void LogSubscriptionRejected(
        this ISilverbackLogger logger,
        string topicPattern,
        MqttClientSubscribeResultCode resultCode,
        MqttClientWrapper client) =>
        SubscriptionRejected(
            logger.InnerLogger,
            topicPattern,
            resultCode,
            client.DisplayName,
            client.Configuration.ClientId);

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

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message is consumed from a MQTT topic.
    /// </summary>
    [LoggerMessage(
        4011,
        LogLevel.Debug,
        "Consuming message {BrokerMessageId} from topic {Topic} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.MQTT_ConsumingMessage")]
    private static partial void ConsumingMessage(ILogger logger, string brokerMessageId, string topic, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message couldn't be acknowledged.
    /// </summary>
    [LoggerMessage(
        4012,
        LogLevel.Warning,
        "Failed to acknowledge message {BrokerMessageId} from topic {Topic} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.MQTT_AcknowledgeFailed")]
    private static partial void AcknowledgeFailed(
        ILogger logger,
        string brokerMessageId,
        string topic,
        string consumerName,
        Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when no matching subscribers are found for a produced message.
    /// </summary>
    [LoggerMessage(
        4013,
        LogLevel.Warning,
        "No matching subscribers found for the produced message | Topic: {Topic}",
        EventName = "Silverback.Integration.MQTT_NoMatchingSubscribers")]
    private static partial void NoMatchingSubscribers(ILogger logger, string topic);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a consumer with batch processing and QoS 1 or 2
    ///     is created. The messages are acknowledged when their batch is processed, and the broker stops delivering when too many
    ///     messages are waiting for acknowledgment: the broker must allow at least the logged number of unacknowledged messages.
    /// </summary>
    [LoggerMessage(
        4014,
        LogLevel.Information,
        "Batch processing enabled: the broker must allow at least {MaxPendingMessages} unacknowledged messages " +
        "(batch size {BatchSize}, max degree of parallelism {MaxDegreeOfParallelism}) | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.MQTT_BatchProcessingEnabled")]
    private static partial void BatchProcessingEnabled(
        ILogger logger,
        long maxPendingMessages,
        int batchSize,
        int maxDegreeOfParallelism,
        string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs while connecting to the MQTT broker.
    /// </summary>
    [LoggerMessage(
        4021,
        LogLevel.Warning,
        "Error occurred connecting to MQTT | ClientName: {ClientName}, ClientId: {ClientId}, Broker: {Broker}",
        EventName = "Silverback.Integration.MQTT_ConnectError")]
    private static partial void ConnectError(ILogger logger, string clientName, string clientId, string broker, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs while retrying to connect to the
    ///     MQTT broker.
    /// </summary>
    [LoggerMessage(
        4022,
        LogLevel.Debug,
        "Error occurred retrying to connect to MQTT | ClientName: {ClientName}, ClientId: {ClientId}, Broker: {Broker}",
        EventName = "Silverback.Integration.MQTT_ConnectRetryError")]
    private static partial void ConnectRetryError(ILogger logger, string clientName, string clientId, string broker, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the connection to the MQTT
    ///     broker is lost.
    /// </summary>
    [LoggerMessage(
        4023,
        LogLevel.Warning,
        "Connection to MQTT lost; the client will try to reconnect | ClientName: {ClientName}, ClientId: {ClientId}, Broker: {Broker}",
        EventName = "Silverback.Integration.MQTT_ConnectionLost")]
    private static partial void ConnectionLost(ILogger logger, string clientName, string clientId, string broker);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the connection to the MQTT
    ///     broker is established again after it was lost.
    /// </summary>
    [LoggerMessage(
        4024,
        LogLevel.Information,
        "Connection to MQTT reestablished | ClientName: {ClientName}, ClientId: {ClientId}, Broker: {Broker}",
        EventName = "Silverback.Integration.MQTT_Reconnected")]
    private static partial void Reconnected(ILogger logger, string clientName, string clientId, string broker);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the processing of the producer
    ///     queue is being stopped (usually because the application is exiting).
    /// </summary>
    [LoggerMessage(
        4031,
        LogLevel.Debug,
        "Producer queue processing canceled | ClientName: {ClientName}, ClientId: {ClientId}",
        EventName = "Silverback.Integration.MQTT_ProducerQueueProcessingCanceled")]
    private static partial void ProducerQueueProcessingCanceled(ILogger logger, string clientName, string clientId);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the consumer subscribes to a topic or pattern.
    /// </summary>
    [LoggerMessage(
        4041,
        LogLevel.Information,
        "Consumer subscribed to {TopicPattern} | ClientName: {ClientName}, ClientId: {ClientId}, ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.MQTT_ConsumerSubscribed")]
    private static partial void ConsumerSubscribed(
        ILogger logger,
        string topicPattern,
        string clientName,
        string clientId,
        string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the broker rejects the subscription to a topic or
    ///     pattern.
    /// </summary>
    [LoggerMessage(
        4042,
        LogLevel.Error,
        "Subscription to {TopicPattern} rejected by the broker with result code {ResultCode} | ClientName: {ClientName}, ClientId: {ClientId}",
        EventName = "Silverback.Integration.MQTT_SubscriptionRejected")]
    private static partial void SubscriptionRejected(
        ILogger logger,
        string topicPattern,
        MqttClientSubscribeResultCode resultCode,
        string clientName,
        string clientId);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="MqttClient" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        4101,
        LogLevel.Error,
        "Error from MqttClient ({Source}): '{LogMessage}'",
        EventName = "Silverback.Integration.MQTT_MqttClientLogError")]
    private static partial void MqttClientLogError(ILogger logger, string source, string logMessage, Exception? exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="MqttClient" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        4102,
        LogLevel.Warning,
        "Warning from MqttClient ({Source}): '{LogMessage}'",
        EventName = "Silverback.Integration.MQTT_MqttClientLogWarning")]
    private static partial void MqttClientLogWarning(ILogger logger, string source, string logMessage, Exception? exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="MqttClient" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        4103,
        LogLevel.Information,
        "Information from MqttClient ({Source}): '{LogMessage}'",
        EventName = "Silverback.Integration.MQTT_MqttClientLogInformation")]
    private static partial void MqttClientLogInformation(ILogger logger, string source, string logMessage, Exception? exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="MqttClient" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        4104,
        LogLevel.Trace,
        "Verbose from MqttClient ({Source}): '{LogMessage}'",
        EventName = "Silverback.Integration.MQTT_MqttClientLogVerbose")]
    private static partial void MqttClientLogVerbose(ILogger logger, string source, string logMessage, Exception? exception);
}
