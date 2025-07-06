// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json.Serialization;
using Silverback.TestBench.Models;
using Silverback.TestBench.ViewModel.Topics;

namespace Silverback.TestBench.Producer;

public class RoutableTestBenchMessage : TestBenchMessage
{
    private static readonly Random Random = new();

    public RoutableTestBenchMessage(TopicViewModel targetTopicConfiguration)
    {
        TargetTopicName = targetTopicConfiguration.TopicName;

        double produceDelayTotalMilliseconds = targetTopicConfiguration.ProduceDelay.TotalMilliseconds;
        SimulatedProcessingTime = TimeSpan.FromMilliseconds(Random.Next(0, (int)(produceDelayTotalMilliseconds * 0.9)));

        if (Random.NextDouble() < targetTopicConfiguration.SimulateErrorProbability)
            SimulatedFailuresCount = Random.Next(1, 3);

        MessageId = Guid.NewGuid().ToString("N");

        CreatedAt = DateTime.UtcNow;
    }

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string TargetTopicName { get; }
}
