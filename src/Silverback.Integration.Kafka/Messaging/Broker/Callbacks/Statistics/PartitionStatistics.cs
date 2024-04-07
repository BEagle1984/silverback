// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable 1591 // Will maybe document later

namespace Silverback.Messaging.Broker.Callbacks.Statistics;

[SuppressMessage("ReSharper", "SA1600", Justification = "Will maybe document later")]
public class PartitionStatistics
{
    [JsonPropertyName("partition")]
    public long Partition { get; init; }

    [JsonPropertyName("broker")]
    public long Broker { get; init; }

    [JsonPropertyName("leader")]
    public long Leader { get; init; }

    [JsonPropertyName("desired")]
    public bool Desired { get; init; }

    [JsonPropertyName("unknown")]
    public bool Unknown { get; init; }

    [JsonPropertyName("msgq_cnt")]
    public long MsgqCnt { get; init; }

    [JsonPropertyName("msgq_bytes")]
    public long MsgqBytes { get; init; }

    [JsonPropertyName("xmit_msgq_cnt")]
    public long XmitMsgqCnt { get; init; }

    [JsonPropertyName("xmit_msgq_bytes")]
    public long XmitMsgqBytes { get; init; }

    [JsonPropertyName("fetchq_cnt")]
    public long FetchqCnt { get; init; }

    [JsonPropertyName("fetchq_size")]
    public long FetchqSize { get; init; }

    [JsonPropertyName("fetch_state")]
    public string FetchState { get; init; } = string.Empty;

    [JsonPropertyName("query_offset")]
    public long QueryOffset { get; init; }

    [JsonPropertyName("next_offset")]
    public long NextOffset { get; init; }

    [JsonPropertyName("app_offset")]
    public long AppOffset { get; init; }

    [JsonPropertyName("stored_offset")]
    public long StoredOffset { get; init; }

    [JsonPropertyName("commited_offset")]
    public long CommitedOffset { get; init; }

    [JsonPropertyName("committed_offset")]
    public long CommittedOffset { get; init; }

    [JsonPropertyName("eof_offset")]
    public long EofOffset { get; init; }

    [JsonPropertyName("lo_offset")]
    public long LoOffset { get; init; }

    [JsonPropertyName("hi_offset")]
    public long HiOffset { get; init; }

    [JsonPropertyName("ls_offset")]
    public long LsOffset { get; init; }

    [JsonPropertyName("consumer_lag")]
    public long ConsumerLag { get; init; }

    [JsonPropertyName("txmsgs")]
    public long TxMsgs { get; init; }

    [JsonPropertyName("txbytes")]
    public long TxBytes { get; init; }

    [JsonPropertyName("rxmsgs")]
    public long RxMsgs { get; init; }

    [JsonPropertyName("rxbytes")]
    public long RxBytes { get; init; }

    [JsonPropertyName("msgs")]
    public long Msgs { get; init; }

    [JsonPropertyName("rx_ver_drops")]
    public long RxVerDrops { get; init; }

    [JsonPropertyName("msgs_inflight")]
    public long MsgsInflight { get; init; }

    [JsonPropertyName("next_ack_seq")]
    public long NextAckSeq { get; init; }

    [JsonPropertyName("next_err_seq")]
    public long NextErrSeq { get; init; }

    [JsonPropertyName("acked_msgid")]
    public long AckedMsgId { get; init; }
}
