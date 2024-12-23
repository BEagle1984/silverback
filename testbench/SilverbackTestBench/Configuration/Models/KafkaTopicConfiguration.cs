// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.TestBench.Configuration.Models;

public record KafkaTopicConfiguration(
    string TopicName,
    int PartitionsCount,
    TimeSpan ProduceDelay,
    double SimulatedFailureChance = 0.001,
    bool Enabled = true)
    : TopicConfiguration(TopicName, ProduceDelay, SimulatedFailureChance, Enabled)
{
    public override string ToString() => base.ToString();
}
