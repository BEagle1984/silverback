// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

#pragma warning disable 1591 // Will maybe document later

namespace Silverback.Messaging.Messages.Statistics
{
    [SuppressMessage("ReSharper", "SA1600", Justification = "Will maybe document later")]
    public class ConsumerGroupStatistics
    {
        [JsonProperty("state")]
        public string State { get; set; } = string.Empty;

        [JsonProperty("stateage")]
        public long StateAge { get; set; }

        [JsonProperty("join_state")]
        public string JoinState { get; set; } = string.Empty;

        [JsonProperty("rebalance_age")]
        public long RebalanceAge { get; set; }

        [JsonProperty("rebalance_cnt")]
        public long RebalanceCnt { get; set; }

        [JsonProperty("rebalance_reason")]
        public string RebalanceReason { get; set; } = string.Empty;

        [JsonProperty("assignment_size")]
        public long AssignmentSize { get; set; }
    }
}