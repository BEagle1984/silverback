﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable 1591 // Will maybe document later

namespace Silverback.Messaging.KafkaEvents.Statistics
{
    [SuppressMessage("ReSharper", "SA1600", Justification = "Will maybe document later")]
    public class TopicPartitions
    {
        [JsonPropertyName("topic")]
        public string Topic { get; set; } = string.Empty;

        [JsonPropertyName("partition")]
        public long Partition { get; set; }
    }
}
