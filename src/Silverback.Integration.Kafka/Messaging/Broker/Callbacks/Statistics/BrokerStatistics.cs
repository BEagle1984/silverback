// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable 1591 // Will maybe document later

namespace Silverback.Messaging.Broker.Callbacks.Statistics;

[SuppressMessage("ReSharper", "SA1600", Justification = "Will maybe document later")]
public class BrokerStatistics
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("nodeid")]
    public long NodeId { get; init; }

    [JsonPropertyName("nodename")]
    public string NodeName { get; init; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; init; } = string.Empty;

    [JsonPropertyName("stateage")]
    public long StateAge { get; init; }

    [JsonPropertyName("outbuf_cnt")]
    public long OutbufCnt { get; init; }

    [JsonPropertyName("outbuf_msg_cnt")]
    public long OutbufMsgCnt { get; init; }

    [JsonPropertyName("waitresp_cnt")]
    public long WaitRespCnt { get; init; }

    [JsonPropertyName("waitresp_msg_cnt")]
    public long WaitRespMsgCnt { get; init; }

    [JsonPropertyName("tx")]
    public long Tx { get; init; }

    [JsonPropertyName("txbytes")]
    public long TxBytes { get; init; }

    [JsonPropertyName("txerrs")]
    public long TxErrs { get; init; }

    [JsonPropertyName("txretries")]
    public long TxRetries { get; init; }

    [JsonPropertyName("req_timeouts")]
    public long ReqTimeouts { get; init; }

    [JsonPropertyName("rx")]
    public long Rx { get; init; }

    [JsonPropertyName("rxbytes")]
    public long RxBytes { get; init; }

    [JsonPropertyName("rxerrs")]
    public long RxErrs { get; init; }

    [JsonPropertyName("rxcorriderrs")]
    public long RxCorriderrs { get; init; }

    [JsonPropertyName("rxpartial")]
    public long RxPartial { get; init; }

    [JsonPropertyName("zbuf_grow")]
    public long ZBufGrow { get; init; }

    [JsonPropertyName("buf_grow")]
    public long BufGrow { get; init; }

    [JsonPropertyName("wakeups")]
    public long Wakeups { get; init; }

    [JsonPropertyName("connects")]
    public long Connects { get; init; }

    [JsonPropertyName("disconnects")]
    public long Disconnects { get; init; }

    [JsonPropertyName("int_latency")]
    public WindowStatistics IntLatency { get; init; } = new();

    [JsonPropertyName("outbuf_latency")]
    public WindowStatistics OutbufLatency { get; init; } = new();

    [JsonPropertyName("rtt")]
    public WindowStatistics Rtt { get; init; } = new();

    [JsonPropertyName("throttle")]
    public WindowStatistics Throttle { get; init; } = new();

    [JsonPropertyName("req")]
    public Dictionary<string, long> Requests { get; init; } = [];

    [JsonPropertyName("toppars")]
    public Dictionary<string, TopicPartitions> TopicPartitions { get; init; } = [];
}
