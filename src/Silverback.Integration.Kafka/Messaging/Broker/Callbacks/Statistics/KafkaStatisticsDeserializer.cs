// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Silverback.Diagnostics;

namespace Silverback.Messaging.Broker.Callbacks.Statistics;

internal static class KafkaStatisticsDeserializer
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    public static KafkaStatistics? TryDeserialize(string json, ISilverbackLogger logger)
    {
        try
        {
            return JsonSerializer.Deserialize<KafkaStatistics>(json) ??
                   throw new InvalidOperationException("Failed to deserialize Kafka statistics.");
        }
        catch (Exception ex)
        {
            logger.LogStatisticsDeserializationError(ex);
            return null;
        }
    }
}
