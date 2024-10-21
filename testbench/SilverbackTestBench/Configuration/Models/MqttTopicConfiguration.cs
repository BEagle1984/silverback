// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.TestBench.Configuration.Models;

public record MqttTopicConfiguration(
    string TopicName,
    TimeSpan ProduceDelay,
    double SimulatedFailureChance = 0.01,
    bool Enabled = true)
    : TopicConfiguration(TopicName, ProduceDelay, SimulatedFailureChance, Enabled)
{
    public override string ToString() => base.ToString();
}
