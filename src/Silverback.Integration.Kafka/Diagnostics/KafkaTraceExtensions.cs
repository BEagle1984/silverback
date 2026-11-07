// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;

namespace Silverback.Diagnostics;

// The traces are diagnostics, not a public contract: they share the IntegrationLogEvents.Tracing event id and, unlike the
// *LoggerExtensions classes, generate no public LogEvent
internal static partial class KafkaTraceExtensions
{
    public static void LogConsumeLoopHandlerStopping(this ISilverbackLogger logger, IConsumer consumer, string instanceId) =>
        ConsumeLoopHandlerStopping(logger.InnerLogger, instanceId, consumer.DisplayName);

    public static void LogConsumeLoopHandlerAwaitingStoppingToken(this ISilverbackLogger logger, IConsumer consumer, string instanceId) =>
        ConsumeLoopHandlerAwaitingStoppingToken(logger.InnerLogger, instanceId, consumer.DisplayName);

    public static void LogConsumeLoopHandlerDisposing(this ISilverbackLogger logger, IConsumer consumer, string instanceId) =>
        ConsumeLoopHandlerDisposing(logger.InnerLogger, instanceId, consumer.DisplayName);

    public static void LogConsumeLoopHandlerDisposed(this ISilverbackLogger logger, IConsumer consumer, string instanceId) =>
        ConsumeLoopHandlerDisposed(logger.InnerLogger, instanceId, consumer.DisplayName);

    public static void LogConsumeLoopStarting(this ISilverbackLogger logger, IConsumer consumer, string instanceId, int taskId) =>
        ConsumeLoopStarting(logger.InnerLogger, instanceId, taskId, consumer.DisplayName);

    public static void LogConsumeLoopStopped(this ISilverbackLogger logger, IConsumer consumer, string instanceId, int taskId) =>
        ConsumeLoopStopped(logger.InnerLogger, instanceId, taskId, consumer.DisplayName);

    public static void LogConsumeLoopHandlerStarted(this ISilverbackLogger logger, IConsumer consumer, string instanceId, int taskId) =>
        ConsumeLoopHandlerStarted(logger.InnerLogger, instanceId, taskId, consumer.DisplayName);

    public static void LogConsumeLoopHandlerStopAwaiting(this ISilverbackLogger logger, IConsumer consumer, string instanceId, int taskId) =>
        ConsumeLoopHandlerStopAwaiting(logger.InnerLogger, instanceId, taskId, consumer.DisplayName);

    public static void LogConsumeLoopHandlerStopped(this ISilverbackLogger logger, IConsumer consumer, string instanceId, int taskId) =>
        ConsumeLoopHandlerStopped(logger.InnerLogger, instanceId, taskId, consumer.DisplayName);

    public static void LogChannelsManagerStopAwaiting(this ISilverbackLogger logger, IConsumer consumer) =>
        ChannelsManagerStopAwaiting(logger.InnerLogger, consumer.DisplayName);

    public static void LogChannelsManagerStopped(this ISilverbackLogger logger, IConsumer consumer) =>
        ChannelsManagerStopped(logger.InnerLogger, consumer.DisplayName);

    public static void LogMessageWritingToChannel(
        this ISilverbackLogger logger,
        IConsumer consumer,
        ConsumeResult<byte[]?, byte[]?> consumeResult,
        IConsumerChannel channel) =>
        MessageWritingToChannel(
            logger.InnerLogger,
            consumeResult.Topic,
            consumeResult.Partition.Value,
            consumeResult.Offset.Value,
            channel.Id,
            consumer.DisplayName);

    public static void LogMessageWriteToOverflowChannelFailed(
        this ISilverbackLogger logger,
        IConsumer consumer,
        ConsumeResult<byte[]?, byte[]?> consumeResult,
        IConsumerChannel channel) =>
        MessageWriteToOverflowChannelFailed(
            logger.InnerLogger,
            consumeResult.Topic,
            consumeResult.Partition.Value,
            consumeResult.Offset.Value,
            channel.Id,
            consumer.DisplayName);

    public static void LogPolledMessageDropped(
        this ISilverbackLogger logger,
        IConsumer consumer,
        ConsumeResult<byte[]?, byte[]?> consumeResult) =>
        PolledMessageDropped(
            logger.InnerLogger,
            consumeResult.Topic,
            consumeResult.Partition.Value,
            consumeResult.Offset.Value,
            consumer.DisplayName);

    public static void LogOffsetStoring(this ISilverbackLogger logger, IConsumer consumer, TopicPartitionOffset offset) =>
        OffsetStoring(logger.InnerLogger, offset.Topic, offset.Partition.Value, offset.Offset.Value, consumer.DisplayName);

    public static void LogRollbackRestartSkipped(this ISilverbackLogger logger, IConsumer consumer) =>
        RollbackRestartSkipped(logger.InnerLogger, consumer.DisplayName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Stopping ConsumeLoopHandler | InstanceId: {InstanceId} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_Tracing_ConsumeLoopHandlerStopping")]
    private static partial void ConsumeLoopHandlerStopping(ILogger logger, string instanceId, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Stopping ConsumeLoopHandler, awaiting stopping token | InstanceId: {InstanceId} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_Tracing_ConsumeLoopHandlerAwaitingStoppingToken")]
    private static partial void ConsumeLoopHandlerAwaitingStoppingToken(ILogger logger, string instanceId, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Disposing ConsumeLoopHandler | InstanceId: {InstanceId} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_Tracing_ConsumeLoopHandlerDisposing")]
    private static partial void ConsumeLoopHandlerDisposing(ILogger logger, string instanceId, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "ConsumeLoopHandler disposed | InstanceId: {InstanceId} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_Tracing_ConsumeLoopHandlerDisposed")]
    private static partial void ConsumeLoopHandlerDisposed(ILogger logger, string instanceId, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Starting consume loop | InstanceId: {InstanceId}, TaskId: {TaskId} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_Tracing_ConsumeLoopStarting")]
    private static partial void ConsumeLoopStarting(ILogger logger, string instanceId, int taskId, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Consume loop stopped | InstanceId: {InstanceId}, TaskId: {TaskId} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_Tracing_ConsumeLoopStopped")]
    private static partial void ConsumeLoopStopped(ILogger logger, string instanceId, int taskId, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "ConsumeLoopHandler started | InstanceId: {InstanceId}, TaskId: {TaskId} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_Tracing_ConsumeLoopHandlerStarted")]
    private static partial void ConsumeLoopHandlerStarted(ILogger logger, string instanceId, int taskId, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Waiting ConsumeLoopHandler stop | InstanceId: {InstanceId}, TaskId: {TaskId} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_Tracing_ConsumeLoopHandlerStopAwaiting")]
    private static partial void ConsumeLoopHandlerStopAwaiting(ILogger logger, string instanceId, int taskId, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "ConsumeLoopHandler stopped | InstanceId: {InstanceId}, TaskId: {TaskId}. | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_Tracing_ConsumeLoopHandlerStopped")]
    private static partial void ConsumeLoopHandlerStopped(ILogger logger, string instanceId, int taskId, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Waiting ChannelsManager stop | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_Tracing_ChannelsManagerStopAwaiting")]
    private static partial void ChannelsManagerStopAwaiting(ILogger logger, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "ChannelsManager stopped | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_Tracing_ChannelsManagerStopped")]
    private static partial void ChannelsManagerStopped(ILogger logger, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Writing message ({Topic}[{Partition}]@{Offset}) to channel {Channel} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_Tracing_MessageWritingToChannel")]
    private static partial void MessageWritingToChannel(
        ILogger logger,
        string topic,
        int partition,
        long offset,
        string channel,
        string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Failed to write message ({Topic}[{Partition}]@{Offset}) to overflow channel {Channel} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_Tracing_MessageWriteToOverflowChannelFailed")]
    private static partial void MessageWriteToOverflowChannelFailed(
        ILogger logger,
        string topic,
        int partition,
        long offset,
        string channel,
        string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Dropping message ({Topic}[{Partition}]@{Offset}) polled while stopping: it will be consumed again | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_Tracing_PolledMessageDropped")]
    private static partial void PolledMessageDropped(ILogger logger, string topic, int partition, long offset, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Storing offset {Topic}[{Partition}]@{Offset} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_Tracing_OffsetStoring")]
    private static partial void OffsetStoring(ILogger logger, string topic, int partition, long offset, string consumerName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Skipping rollback restart for obsolete partition assignments | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_Tracing_RollbackRestartSkipped")]
    private static partial void RollbackRestartSkipped(ILogger logger, string consumerName);
}
