// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.TestBench.ViewModel.Framework;

namespace Silverback.TestBench.ViewModel.Topics;

public abstract class TopicViewModel : ViewModelBase
{
    private TimeSpan _produceDelay;

    private double _simulateErrorProbability;

    private bool _isEnabled;

    protected TopicViewModel(
        string topicName,
        TimeSpan produceDelay,
        double simulateErrorProbability,
        bool isEnabled)
    {
        TopicName = topicName;
        _produceDelay = produceDelay;
        _simulateErrorProbability = simulateErrorProbability;
        _isEnabled = isEnabled;
    }

    public string TopicName { get; }

    public TimeSpan ProduceDelay
    {
        get => _produceDelay;
        set => SetProperty(ref _produceDelay, value, nameof(ProduceDelay));
    }

    public double SimulateErrorProbability
    {
        get => _simulateErrorProbability;
        set => SetProperty(ref _simulateErrorProbability, value, nameof(SimulateErrorProbability));
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value, nameof(IsEnabled));
    }

    public TopicStatisticsViewModel Statistics { get; } = new();
}
