// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

#pragma warning disable 1591 // Will maybe document later

namespace Silverback.Messaging.Messages.Statistics
{
    /// <summary>
    ///     A Kafka statistics event. See
    ///     <see href="https://github.com/edenhill/librdkafka/blob/master/STATISTICS.md" /> for information
    ///     about the structure.
    /// </summary>
    [SuppressMessage("ReSharper", "SA1600", Justification = "Will maybe document later")]
    public class KafkaStatistics
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("client_id")]
        public string ClientId { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("ts")]
        public long Ts { get; set; }

        [JsonProperty("time")]
        public long Time { get; set; }

        [JsonProperty("replyq")]
        public long ReplyQ { get; set; }

        [JsonProperty("msg_cnt")]
        public long MsgCnt { get; set; }

        [JsonProperty("msg_size")]
        public long MsgSize { get; set; }

        [JsonProperty("msg_max")]
        public long MsgMax { get; set; }

        [JsonProperty("msg_size_max")]
        public long MsgSizeMax { get; set; }

        [JsonProperty("simple_cnt")]
        public long SimpleCnt { get; set; }

        [JsonProperty("metadata_cache_cnt")]
        public long MetadataCacheCnt { get; set; }

        [JsonProperty("brokers")]
        [SuppressMessage("ReSharper", "CA2227", Justification = "DTO")]
        public Dictionary<string, BrokerStatistics> Brokers { get; set; } = new Dictionary<string, BrokerStatistics>();

        [JsonProperty("topics")]
        [SuppressMessage("ReSharper", "CA2227", Justification = "DTO")]
        public Dictionary<string, TopicStatistics> Topics { get; set; } = new Dictionary<string, TopicStatistics>();

        [JsonProperty("cgrp")]
        public ConsumerGroupStatistics ConsumerGroup { get; set; } = new ConsumerGroupStatistics();

        [JsonProperty("eos")]
        public ExactlyOnceSemanticsStatistics ExactlyOnceSemantics { get; set; } = new ExactlyOnceSemanticsStatistics();

        [JsonProperty("tx")]
        public long Tx { get; set; }

        [JsonProperty("tx_bytes")]
        public long TxBytes { get; set; }

        [JsonProperty("rx")]
        public long Rx { get; set; }

        [JsonProperty("rx_bytes")]
        public long RxBytes { get; set; }

        [JsonProperty("txmsgs")]
        public long TxMsgs { get; set; }

        [JsonProperty("txmsg_bytes")]
        public long TxMsgBytes { get; set; }

        [JsonProperty("rxmsgs")]
        public long RxMsgs { get; set; }

        [JsonProperty("rxmsg_bytes")]
        public long RxMsgBytes { get; set; }
    }
}
