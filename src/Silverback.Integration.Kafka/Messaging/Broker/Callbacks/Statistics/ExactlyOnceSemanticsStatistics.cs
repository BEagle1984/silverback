// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable 1591 // Will maybe document later

namespace Silverback.Messaging.Broker.Callbacks.Statistics;

[SuppressMessage("ReSharper", "SA1600", Justification = "Will maybe document later")]
public class ExactlyOnceSemanticsStatistics
{
    [JsonPropertyName("idemp_state")]
    public string IdempState { get; set; } = string.Empty;

    [JsonPropertyName("idemp_stateage")]
    public long IdempStateAge { get; set; }

    [JsonPropertyName("txn_state")]
    public string TxnState { get; set; } = string.Empty;

    [JsonPropertyName("txn_stateage")]
    public long TxnStateAge { get; set; }

    [JsonPropertyName("txn_may_enq")]
    public bool TxnMayEnq { get; set; }

    [JsonPropertyName("producer_id")]
    public long ProducerId { get; set; }

    [JsonPropertyName("producer_epoch")]
    public long ProducerEpoch { get; set; }

    [JsonPropertyName("epoch_cnt")]
    public long EpochCnt { get; set; }
}
