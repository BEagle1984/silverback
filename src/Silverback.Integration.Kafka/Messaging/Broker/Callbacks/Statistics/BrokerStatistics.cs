// Copyright (c) 2020 Sergio Aquilini
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
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("nodeid")]
    public long NodeId { get; set; }

    [JsonPropertyName("nodename")]
    public string NodeName { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("stateage")]
    public long StateAge { get; set; }

    [JsonPropertyName("outbuf_cnt")]
    public long OutbufCnt { get; set; }

    [JsonPropertyName("outbuf_msg_cnt")]
    public long OutbufMsgCnt { get; set; }

    [JsonPropertyName("waitresp_cnt")]
    public long WaitRespCnt { get; set; }

    [JsonPropertyName("waitresp_msg_cnt")]
    public long WaitRespMsgCnt { get; set; }

    [JsonPropertyName("tx")]
    public long Tx { get; set; }

    [JsonPropertyName("txbytes")]
    public long TxBytes { get; set; }

    [JsonPropertyName("txerrs")]
    public long TxErrs { get; set; }

    [JsonPropertyName("txretries")]
    public long TxRetries { get; set; }

    [JsonPropertyName("req_timeouts")]
    public long ReqTimeouts { get; set; }

    [JsonPropertyName("rx")]
    public long Rx { get; set; }

    [JsonPropertyName("rxbytes")]
    public long RxBytes { get; set; }

    [JsonPropertyName("rxerrs")]
    public long RxErrs { get; set; }

    [JsonPropertyName("rxcorriderrs")]
    public long RxCorriderrs { get; set; }

    [JsonPropertyName("rxpartial")]
    public long RxPartial { get; set; }

    [JsonPropertyName("zbuf_grow")]
    public long ZBufGrow { get; set; }

    [JsonPropertyName("buf_grow")]
    public long BufGrow { get; set; }

    [JsonPropertyName("wakeups")]
    public long Wakeups { get; set; }

    [JsonPropertyName("connects")]
    public long Connects { get; set; }

    [JsonPropertyName("disconnects")]
    public long Disconnects { get; set; }

    [JsonPropertyName("int_latency")]
    public WindowStatistics IntLatency { get; set; } = new();

    [JsonPropertyName("outbuf_latency")]
    public WindowStatistics OutbufLatency { get; set; } = new();

    [JsonPropertyName("rtt")]
    public WindowStatistics Rtt { get; set; } = new();

    [JsonPropertyName("throttle")]
    public WindowStatistics Throttle { get; set; } = new();

    [JsonPropertyName("req")]
    [SuppressMessage("", "CA2227", Justification = "DTO")]
    public Dictionary<string, long> Requests { get; set; } = new();

    [JsonPropertyName("toppars")]
    [SuppressMessage("", "CA2227", Justification = "DTO")]
    public Dictionary<string, TopicPartitions> TopicPartitions { get; set; } = new();
}
