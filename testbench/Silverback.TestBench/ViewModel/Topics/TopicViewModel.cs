// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.TestBench.ViewModel.Framework;

namespace Silverback.TestBench.ViewModel.Topics;

public abstract class TopicViewModel : ViewModelBase
{
    protected TopicViewModel(
        string topicName,
        TimeSpan produceDelay,
        double simulateErrorProbability,
        bool isEnabled,
        bool simulateProcessingTime,
        bool simulateErrors)
    {
        TopicName = topicName;
        ProduceDelay = produceDelay;
        SimulateErrorProbability = simulateErrorProbability;
        IsEnabled = isEnabled;
        SimulateProcessingTime = simulateProcessingTime;
        SimulateErrors = simulateErrors;
    }

    public string TopicName { get; }

    public TimeSpan ProduceDelay
    {
        get;
        set => SetProperty(ref field, value, nameof(ProduceDelay));
    }

    public bool SimulateProcessingTime
    {
        get;
        set => SetProperty(ref field, value, nameof(SimulateProcessingTime));
    }

    public bool SimulateErrors
    {
        get;
        set => SetProperty(ref field, value, nameof(SimulateErrors));
    }

    public double SimulateErrorProbability
    {
        get;
        set => SetProperty(ref field, value, nameof(SimulateErrorProbability));
    }

    public bool IsEnabled
    {
        get;
        set => SetProperty(ref field, value, nameof(IsEnabled));
    }

    public TopicStatisticsViewModel Statistics { get; } = new();
}
