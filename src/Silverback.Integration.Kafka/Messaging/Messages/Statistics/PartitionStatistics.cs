using Newtonsoft.Json;

namespace Silverback.Messaging.Messages.Statistics
{
    public class PartitionStatistics
    {
        [JsonProperty("partition")]
        public long Partition { get; set; }

        [JsonProperty("broker")]
        public long Broker { get; set; }

        [JsonProperty("leader")]
        public long Leader { get; set; }

        [JsonProperty("desired")]
        public bool Desired { get; set; }

        [JsonProperty("unknown")]
        public bool Unknown { get; set; }

        [JsonProperty("msgq_cnt")]
        public long MsgqCnt { get; set; }

        [JsonProperty("msgq_bytes")]
        public long MsgqBytes { get; set; }

        [JsonProperty("xmit_msgq_cnt")]
        public long XmitMsgqCnt { get; set; }

        [JsonProperty("xmit_msgq_bytes")]
        public long XmitMsgqBytes { get; set; }

        [JsonProperty("fetchq_cnt")]
        public long FetchqCnt { get; set; }

        [JsonProperty("fetchq_size")]
        public long FetchqSize { get; set; }

        [JsonProperty("fetch_state")]
        public string FetchState { get; set; } = string.Empty;

        [JsonProperty("query_offset")]
        public long QueryOffset { get; set; }

        [JsonProperty("next_offset")]
        public long NextOffset { get; set; }

        [JsonProperty("app_offset")]
        public long AppOffset { get; set; }

        [JsonProperty("stored_offset")]
        public long StoredOffset { get; set; }

        [JsonProperty("commited_offset")]
        public long CommitedOffset { get; set; }

        [JsonProperty("committed_offset")]
        public long CommittedOffset { get; set; }

        [JsonProperty("eof_offset")]
        public long EofOffset { get; set; }

        [JsonProperty("lo_offset")]
        public long LoOffset { get; set; }

        [JsonProperty("hi_offset")]
        public long HiOffset { get; set; }

        [JsonProperty("ls_offset")]
        public long LsOffset { get; set; }

        [JsonProperty("consumer_lag")]
        public long ConsumerLag { get; set; }

        [JsonProperty("txmsgs")]
        public long TxMsgs { get; set; }

        [JsonProperty("txbytes")]
        public long TxBytes { get; set; }

        [JsonProperty("rxmsgs")]
        public long RxMsgs { get; set; }

        [JsonProperty("rxbytes")]
        public long RxBytes { get; set; }

        [JsonProperty("msgs")]
        public long Msgs { get; set; }

        [JsonProperty("rx_ver_drops")]
        public long RxVerDrops { get; set; }

        [JsonProperty("msgs_inflight")]
        public long MsgsInflight { get; set; }

        [JsonProperty("next_ack_seq")]
        public long NextAckSeq { get; set; }

        [JsonProperty("next_err_seq")]
        public long NextErrSeq { get; set; }

        [JsonProperty("acked_msgid")]
        public long AckedMsgId { get; set; }
    }
}