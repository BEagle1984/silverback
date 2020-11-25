﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable 1591 // Will maybe document later

namespace Silverback.Messaging.KafkaEvents.Statistics
{
    [SuppressMessage("ReSharper", "SA1600", Justification = "Will maybe document later")]
    public class ConsumerGroupStatistics
    {
        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("stateage")]
        public long StateAge { get; set; }

        [JsonPropertyName("join_state")]
        public string JoinState { get; set; } = string.Empty;

        [JsonPropertyName("rebalance_age")]
        public long RebalanceAge { get; set; }

        [JsonPropertyName("rebalance_cnt")]
        public long RebalanceCnt { get; set; }

        [JsonPropertyName("rebalance_reason")]
        public string RebalanceReason { get; set; } = string.Empty;

        [JsonPropertyName("assignment_size")]
        public long AssignmentSize { get; set; }
    }
}
