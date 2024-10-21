// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.TestBench.Configuration.Models;

public abstract record TopicConfiguration(
    string TopicName,
    TimeSpan ProduceDelay,
    double SimulatedFailureChance,
    bool Enabled)
{
    public override string ToString() => $"{GetType().Name[0]} {TopicName}";
}
