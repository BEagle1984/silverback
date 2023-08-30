// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.TestBench.Producer.Models;

public class MessagesStats
{
    public int ProducedCount { get; set; }

    public int FailedProduceCount { get; set; }

    public int ConsumedCount { get; set; }

    public int ProcessedCount { get; set; }

    public int SkippedCount { get; set; }

    public int LostCount { get; set; }

    public int LagCount => ProducedCount - ProcessedCount - LostCount;

    public Timestamp CurrentLagTime { get; set; }

    public Timestamp MaxLagTime { get; set; }

    public Timestamp MinLagTime { get; set; }

    public Timestamp MeanLagTime { get; set; }
}
