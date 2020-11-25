// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;

namespace Silverback.Messaging.KafkaEvents.Statistics
{
    internal static class KafkaStatisticsDeserializer
    {
        [SuppressMessage("", "CA1031", Justification = "Exception logged")]
        public static KafkaStatistics TryDeserialize(string json, ISilverbackIntegrationLogger logger)
        {
            try
            {
                return JsonSerializer.Deserialize<KafkaStatistics>(json);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    KafkaEventIds.StatisticsDeserializationError,
                    ex,
                    "The received statistics JSON couldn't be deserialized.");

                return new KafkaStatistics();
            }
        }
    }
}
