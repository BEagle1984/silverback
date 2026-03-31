// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json.Serialization;
using Silverback.TestBench.Models;
using Silverback.TestBench.ViewModel.Topics;

namespace Silverback.TestBench.Producer.Messages;

public abstract class RoutableTestBenchMessage : TestBenchMessage
{
    private static readonly Random Random = new();

    protected RoutableTestBenchMessage(TopicViewModel targetTopicConfiguration)
    {
        TargetTopicName = targetTopicConfiguration.TopicName;

        if (targetTopicConfiguration.SimulateProcessingTime)
            SimulatedProcessingTime = TimeSpan.FromMilliseconds(Random.Next(0, (int)(targetTopicConfiguration.ProduceDelay.TotalMilliseconds * 0.7)));

        if (targetTopicConfiguration.SimulateErrors && Random.NextDouble() < targetTopicConfiguration.SimulateErrorProbability)
            SimulatedFailuresCount = Random.Next(1, 3);

        MessageId = Guid.NewGuid().ToString("N");

        CreatedAt = DateTime.UtcNow;
    }

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string TargetTopicName { get; }
}
