// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Newtonsoft.Json;

namespace Silverback.Messaging.Messages.Statistics
{
    public class TopicPartitions
    {
        [JsonProperty("topic")]
        public string Topic { get; set; } = string.Empty;

        [JsonProperty("partition")]
        public long Partition { get; set; }
    }
}