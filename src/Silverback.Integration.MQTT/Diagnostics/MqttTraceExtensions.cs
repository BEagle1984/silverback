// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;

namespace Silverback.Diagnostics;

// The traces are diagnostics, not a public contract: they share the IntegrationLogEvents.Tracing event id and, unlike the
// *LoggerExtensions classes, generate no public LogEvent
internal static partial class MqttTraceExtensions
{
    public static void LogRollbackRestartSkipped(this ISilverbackLogger logger, IConsumer consumer, IConsumerChannel channel) =>
        RollbackRestartSkipped(logger.InnerLogger, channel.Id, consumer.DisplayName);

    [LoggerMessage(
        IntegrationLogEvents.TracingEventId,
        LogLevel.Trace,
        "Skipping rollback restart of channel {Channel} (taken over by the stop) | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.MQTT_Tracing_RollbackRestartSkipped")]
    private static partial void RollbackRestartSkipped(ILogger logger, string channel, string consumerName);
}
