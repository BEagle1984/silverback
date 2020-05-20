// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

#pragma warning disable 1591 // Will maybe document later

namespace Silverback.Messaging.Messages.Statistics
{
    [SuppressMessage("ReSharper", "SA1600", Justification = "Will maybe document later")]
    public class TopicStatistics
    {
        [JsonProperty("topic")]
        public string Topic { get; set; } = string.Empty;

        [JsonProperty("metadata_age")]
        public long MetadataAge { get; set; }

        [JsonProperty("batchsize")]
        public WindowStatistics BatchSize { get; set; } = new WindowStatistics();

        [JsonProperty("batchcnt")]
        public WindowStatistics BatchCnt { get; set; } = new WindowStatistics();

        [JsonProperty("partitions")]
        [SuppressMessage("ReSharper", "CA2227", Justification = "DTO")]
        public Dictionary<string, PartitionStatistics> Partitions { get; set; } =
            new Dictionary<string, PartitionStatistics>();
    }
}