// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

#pragma warning disable 1591 // Will maybe document later

namespace Silverback.Messaging.Messages.Statistics
{
    [SuppressMessage("ReSharper", "SA1600", Justification = "Will maybe document later")]
    public class TopicPartitions
    {
        [JsonProperty("topic")]
        public string Topic { get; set; } = string.Empty;

        [JsonProperty("partition")]
        public long Partition { get; set; }
    }
}