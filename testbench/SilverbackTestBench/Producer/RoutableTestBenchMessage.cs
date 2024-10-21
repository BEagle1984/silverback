// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.TestBench.Configuration.Models;
using Silverback.TestBench.Models;

namespace Silverback.TestBench.Producer;

public class RoutableTestBenchMessage : TestBenchMessage
{
    private static readonly Random Random = new();

    public RoutableTestBenchMessage(TopicConfiguration targetTopicConfiguration)
    {
        TargetTopicConfiguration = targetTopicConfiguration;

        SimulatedProcessingTime = TimeSpan.FromMilliseconds(
            Random.Next(
                0,
                (int)(targetTopicConfiguration.ProduceDelay.TotalMilliseconds * 0.9)));

        if (Random.NextDouble() < targetTopicConfiguration.SimulatedFailureChance)
            SimulatedFailuresCount = Random.Next(1, 3);

        MessageId = Guid.NewGuid().ToString("N");

        CreatedAt = DateTime.Now;
    }

    public TopicConfiguration TargetTopicConfiguration { get; }
}
