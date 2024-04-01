// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable 1591 // Will maybe document later

namespace Silverback.Messaging.Broker.Callbacks.Statistics;

/// <summary>
///     A Kafka statistics event. See
///     <see href="https://github.com/edenhill/librdkafka/blob/master/STATISTICS.md" /> for information
///     about the structure.
/// </summary>
[SuppressMessage("ReSharper", "SA1600", Justification = "Will maybe document later")]
public class KafkaStatistics
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("client_id")]
    public string ClientId { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("ts")]
    public long Ts { get; init; }

    [JsonPropertyName("time")]
    public long Time { get; init; }

    [JsonPropertyName("replyq")]
    public long ReplyQ { get; init; }

    [JsonPropertyName("msg_cnt")]
    public long MsgCnt { get; init; }

    [JsonPropertyName("msg_size")]
    public long MsgSize { get; init; }

    [JsonPropertyName("msg_max")]
    public long MsgMax { get; init; }

    [JsonPropertyName("msg_size_max")]
    public long MsgSizeMax { get; init; }

    [JsonPropertyName("simple_cnt")]
    public long SimpleCnt { get; init; }

    [JsonPropertyName("metadata_cache_cnt")]
    public long MetadataCacheCnt { get; init; }

    [JsonPropertyName("brokers")]
    public Dictionary<string, BrokerStatistics> Brokers { get; init; } = [];

    [JsonPropertyName("topics")]
    public Dictionary<string, TopicStatistics> Topics { get; init; } = [];

    [JsonPropertyName("cgrp")]
    public ConsumerGroupStatistics ConsumerGroup { get; init; } = new();

    [JsonPropertyName("eos")]
    public ExactlyOnceSemanticsStatistics ExactlyOnceSemantics { get; init; } = new();

    [JsonPropertyName("tx")]
    public long Tx { get; init; }

    [JsonPropertyName("tx_bytes")]
    public long TxBytes { get; init; }

    [JsonPropertyName("rx")]
    public long Rx { get; init; }

    [JsonPropertyName("rx_bytes")]
    public long RxBytes { get; init; }

    [JsonPropertyName("txmsgs")]
    public long TxMsgs { get; init; }

    [JsonPropertyName("txmsg_bytes")]
    public long TxMsgBytes { get; init; }

    [JsonPropertyName("rxmsgs")]
    public long RxMsgs { get; init; }

    [JsonPropertyName("rxmsg_bytes")]
    public long RxMsgBytes { get; init; }
}
