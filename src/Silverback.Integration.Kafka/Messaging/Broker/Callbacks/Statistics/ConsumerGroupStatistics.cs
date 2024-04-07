// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable 1591 // Will maybe document later

namespace Silverback.Messaging.Broker.Callbacks.Statistics;

[SuppressMessage("ReSharper", "SA1600", Justification = "Will maybe document later")]
public class ConsumerGroupStatistics
{
    [JsonPropertyName("state")]
    public string State { get; init; } = string.Empty;

    [JsonPropertyName("stateage")]
    public long StateAge { get; init; }

    [JsonPropertyName("join_state")]
    public string JoinState { get; init; } = string.Empty;

    [JsonPropertyName("rebalance_age")]
    public long RebalanceAge { get; init; }

    [JsonPropertyName("rebalance_cnt")]
    public long RebalanceCnt { get; init; }

    [JsonPropertyName("rebalance_reason")]
    public string RebalanceReason { get; init; } = string.Empty;

    [JsonPropertyName("assignment_size")]
    public long AssignmentSize { get; init; }
}
