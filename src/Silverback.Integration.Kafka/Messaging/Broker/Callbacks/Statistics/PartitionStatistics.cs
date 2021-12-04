// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable 1591 // Will maybe document later

namespace Silverback.Messaging.Broker.Callbacks.Statistics;

[SuppressMessage("ReSharper", "SA1600", Justification = "Will maybe document later")]
public class PartitionStatistics
{
    [JsonPropertyName("partition")]
    public long Partition { get; set; }

    [JsonPropertyName("broker")]
    public long Broker { get; set; }

    [JsonPropertyName("leader")]
    public long Leader { get; set; }

    [JsonPropertyName("desired")]
    public bool Desired { get; set; }

    [JsonPropertyName("unknown")]
    public bool Unknown { get; set; }

    [JsonPropertyName("msgq_cnt")]
    public long MsgqCnt { get; set; }

    [JsonPropertyName("msgq_bytes")]
    public long MsgqBytes { get; set; }

    [JsonPropertyName("xmit_msgq_cnt")]
    public long XmitMsgqCnt { get; set; }

    [JsonPropertyName("xmit_msgq_bytes")]
    public long XmitMsgqBytes { get; set; }

    [JsonPropertyName("fetchq_cnt")]
    public long FetchqCnt { get; set; }

    [JsonPropertyName("fetchq_size")]
    public long FetchqSize { get; set; }

    [JsonPropertyName("fetch_state")]
    public string FetchState { get; set; } = string.Empty;

    [JsonPropertyName("query_offset")]
    public long QueryOffset { get; set; }

    [JsonPropertyName("next_offset")]
    public long NextOffset { get; set; }

    [JsonPropertyName("app_offset")]
    public long AppOffset { get; set; }

    [JsonPropertyName("stored_offset")]
    public long StoredOffset { get; set; }

    [JsonPropertyName("commited_offset")]
    public long CommitedOffset { get; set; }

    [JsonPropertyName("committed_offset")]
    public long CommittedOffset { get; set; }

    [JsonPropertyName("eof_offset")]
    public long EofOffset { get; set; }

    [JsonPropertyName("lo_offset")]
    public long LoOffset { get; set; }

    [JsonPropertyName("hi_offset")]
    public long HiOffset { get; set; }

    [JsonPropertyName("ls_offset")]
    public long LsOffset { get; set; }

    [JsonPropertyName("consumer_lag")]
    public long ConsumerLag { get; set; }

    [JsonPropertyName("txmsgs")]
    public long TxMsgs { get; set; }

    [JsonPropertyName("txbytes")]
    public long TxBytes { get; set; }

    [JsonPropertyName("rxmsgs")]
    public long RxMsgs { get; set; }

    [JsonPropertyName("rxbytes")]
    public long RxBytes { get; set; }

    [JsonPropertyName("msgs")]
    public long Msgs { get; set; }

    [JsonPropertyName("rx_ver_drops")]
    public long RxVerDrops { get; set; }

    [JsonPropertyName("msgs_inflight")]
    public long MsgsInflight { get; set; }

    [JsonPropertyName("next_ack_seq")]
    public long NextAckSeq { get; set; }

    [JsonPropertyName("next_err_seq")]
    public long NextErrSeq { get; set; }

    [JsonPropertyName("acked_msgid")]
    public long AckedMsgId { get; set; }
}
