// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

#pragma warning disable 1591 // Will maybe document later

namespace Silverback.Messaging.Messages.Statistics
{
    [SuppressMessage("ReSharper", "SA1600", Justification = "Will maybe document later")]
    public class BrokerStatistics
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("nodeid")]
        public long NodeId { get; set; }

        [JsonProperty("nodename")]
        public string NodeName { get; set; } = string.Empty;

        [JsonProperty("source")]
        public string Source { get; set; } = string.Empty;

        [JsonProperty("state")]
        public string State { get; set; } = string.Empty;

        [JsonProperty("stateage")]
        public long StateAge { get; set; }

        [JsonProperty("outbuf_cnt")]
        public long OutbufCnt { get; set; }

        [JsonProperty("outbuf_msg_cnt")]
        public long OutbufMsgCnt { get; set; }

        [JsonProperty("waitresp_cnt")]
        public long WaitRespCnt { get; set; }

        [JsonProperty("waitresp_msg_cnt")]
        public long WaitRespMsgCnt { get; set; }

        [JsonProperty("tx")]
        public long Tx { get; set; }

        [JsonProperty("txbytes")]
        public long TxBytes { get; set; }

        [JsonProperty("txerrs")]
        public long TxErrs { get; set; }

        [JsonProperty("txretries")]
        public long TxRetries { get; set; }

        [JsonProperty("req_timeouts")]
        public long ReqTimeouts { get; set; }

        [JsonProperty("rx")]
        public long Rx { get; set; }

        [JsonProperty("rxbytes")]
        public long RxBytes { get; set; }

        [JsonProperty("rxerrs")]
        public long RxErrs { get; set; }

        [JsonProperty("rxcorriderrs")]
        public long RxCorriderrs { get; set; }

        [JsonProperty("rxpartial")]
        public long RxPartial { get; set; }

        [JsonProperty("zbuf_grow")]
        public long ZBufGrow { get; set; }

        [JsonProperty("buf_grow")]
        public long BufGrow { get; set; }

        [JsonProperty("wakeups")]
        public long Wakeups { get; set; }

        [JsonProperty("connects")]
        public long Connects { get; set; }

        [JsonProperty("disconnects")]
        public long Disconnects { get; set; }

        [JsonProperty("int_latency")]
        public WindowStatistics IntLatency { get; set; } = new WindowStatistics();

        [JsonProperty("outbuf_latency")]
        public WindowStatistics OutbufLatency { get; set; } = new WindowStatistics();

        [JsonProperty("rtt")]
        public WindowStatistics Rtt { get; set; } = new WindowStatistics();

        [JsonProperty("throttle")]
        public WindowStatistics Throttle { get; set; } = new WindowStatistics();

        [JsonProperty("req")]
        [SuppressMessage("ReSharper", "CA2227", Justification = "DTO")]
        public Dictionary<string, long> Requests { get; set; } = new Dictionary<string, long>();

        [JsonProperty("toppars")]
        [SuppressMessage("ReSharper", "CA2227", Justification = "DTO")]
        public Dictionary<string, TopicPartitions> TopicPartitions { get; set; } =
            new Dictionary<string, TopicPartitions>();
    }
}