// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

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
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("client_id")]
        public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("ts")]
        public long Ts { get; set; }

        [JsonPropertyName("time")]
        public long Time { get; set; }

        [JsonPropertyName("replyq")]
        public long ReplyQ { get; set; }

        [JsonPropertyName("msg_cnt")]
        public long MsgCnt { get; set; }

        [JsonPropertyName("msg_size")]
        public long MsgSize { get; set; }

        [JsonPropertyName("msg_max")]
        public long MsgMax { get; set; }

        [JsonPropertyName("msg_size_max")]
        public long MsgSizeMax { get; set; }

        [JsonPropertyName("simple_cnt")]
        public long SimpleCnt { get; set; }

        [JsonPropertyName("metadata_cache_cnt")]
        public long MetadataCacheCnt { get; set; }

        [JsonPropertyName("brokers")]
        [SuppressMessage("ReSharper", "CA2227", Justification = "DTO")]
        public Dictionary<string, BrokerStatistics> Brokers { get; set; } = new Dictionary<string, BrokerStatistics>();

        [JsonPropertyName("topics")]
        [SuppressMessage("ReSharper", "CA2227", Justification = "DTO")]
        public Dictionary<string, TopicStatistics> Topics { get; set; } = new Dictionary<string, TopicStatistics>();

        [JsonPropertyName("cgrp")]
        public ConsumerGroupStatistics ConsumerGroup { get; set; } = new ConsumerGroupStatistics();

        [JsonPropertyName("eos")]
        public ExactlyOnceSemanticsStatistics ExactlyOnceSemantics { get; set; } = new ExactlyOnceSemanticsStatistics();

        [JsonPropertyName("tx")]
        public long Tx { get; set; }

        [JsonPropertyName("tx_bytes")]
        public long TxBytes { get; set; }

        [JsonPropertyName("rx")]
        public long Rx { get; set; }

        [JsonPropertyName("rx_bytes")]
        public long RxBytes { get; set; }

        [JsonPropertyName("txmsgs")]
        public long TxMsgs { get; set; }

        [JsonPropertyName("txmsg_bytes")]
        public long TxMsgBytes { get; set; }

        [JsonPropertyName("rxmsgs")]
        public long RxMsgs { get; set; }

        [JsonPropertyName("rxmsg_bytes")]
        public long RxMsgBytes { get; set; }
    }
}
