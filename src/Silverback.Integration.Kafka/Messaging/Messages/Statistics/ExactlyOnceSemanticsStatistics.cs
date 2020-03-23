// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Newtonsoft.Json;

namespace Silverback.Messaging.Messages.Statistics
{
    public class ExactlyOnceSemanticsStatistics
    {
        [JsonProperty("idemp_state")]
        public string IdempState { get; set; } = string.Empty;

        [JsonProperty("idemp_stateage")]
        public long IdempStateAge { get; set; }

        [JsonProperty("txn_state")]
        public string TxnState { get; set; } = string.Empty;

        [JsonProperty("txn_stateage")]
        public long TxnStateAge { get; set; }

        [JsonProperty("txn_may_enq")]
        public bool TxnMayEnq { get; set; }

        [JsonProperty("producer_id")]
        public long ProducerId { get; set; }

        [JsonProperty("producer_epoch")]
        public long ProducerEpoch { get; set; }

        [JsonProperty("epoch_cnt")]
        public long EpochCnt { get; set; }
    }
}