// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable 1591 // Will maybe document later

namespace Silverback.Messaging.Broker.Callbacks.Statistics;

[SuppressMessage("ReSharper", "SA1600", Justification = "Will maybe document later")]
public class TopicStatistics
{
    [JsonPropertyName("topic")]
    public string Topic { get; init; } = string.Empty;

    [JsonPropertyName("metadata_age")]
    public long MetadataAge { get; init; }

    [JsonPropertyName("batchsize")]
    public WindowStatistics BatchSize { get; init; } = new();

    [JsonPropertyName("batchcnt")]
    public WindowStatistics BatchCnt { get; init; } = new();

    [JsonPropertyName("partitions")]
    public Dictionary<string, PartitionStatistics> Partitions { get; init; } = new();
}
