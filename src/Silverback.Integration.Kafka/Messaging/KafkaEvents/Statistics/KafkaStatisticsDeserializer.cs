// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Silverback.Diagnostics;

namespace Silverback.Messaging.KafkaEvents.Statistics
{
    internal static class KafkaStatisticsDeserializer
    {
        [SuppressMessage("", "CA1031", Justification = "Exception logged")]
        public static KafkaStatistics TryDeserialize(string json, ISilverbackLogger logger)
        {
            try
            {
                return JsonSerializer.Deserialize<KafkaStatistics>(json) ??
                       throw new InvalidOperationException("Failed to deserialize Kafka statistics.");
            }
            catch (Exception ex)
            {
                logger.LogStatisticsDeserializationError(ex);
                return new KafkaStatistics();
            }
        }
    }
}
