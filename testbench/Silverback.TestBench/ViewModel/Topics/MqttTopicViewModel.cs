// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.TestBench.ViewModel.Topics;

public class MqttTopicViewModel : TopicViewModel
{
    public MqttTopicViewModel(
        string topicName,
        TimeSpan produceDelay,
        double simulateErrorProbability,
        bool isEnabled = true)
        : base(topicName, produceDelay, simulateErrorProbability, isEnabled)
    {
    }
}
