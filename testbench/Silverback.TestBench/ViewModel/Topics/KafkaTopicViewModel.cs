// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.TestBench.ViewModel.Topics;

public class KafkaTopicViewModel : TopicViewModel
{
    private int _partitionsCount;

    public KafkaTopicViewModel(
        string topicName,
        int partitionsCount,
        TimeSpan produceDelay,
        double simulateErrorProbability,
        bool isEnabled = true)
        : base(topicName, produceDelay, simulateErrorProbability, isEnabled)
    {
        _partitionsCount = partitionsCount;
    }

    public int PartitionsCount
    {
        get => _partitionsCount;
        set => SetProperty(ref _partitionsCount, value, nameof(PartitionsCount));
    }
}
