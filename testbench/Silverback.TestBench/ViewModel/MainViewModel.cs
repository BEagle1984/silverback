// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Input;
using Silverback.Messaging.Publishing;
using Silverback.TestBench.Containers.Commands;
using Silverback.TestBench.Utils;
using Silverback.TestBench.ViewModel.Containers;
using Silverback.TestBench.ViewModel.Framework;
using Silverback.TestBench.ViewModel.Logs;
using Silverback.TestBench.ViewModel.Topics;

namespace Silverback.TestBench.ViewModel;

public class MainViewModel : ViewModelBase
{
    private readonly Dictionary<string, TopicViewModel> _topicsDictionary;

    private bool _isProducing;

    private double _produceSpeedMultiplier;

    public MainViewModel(IPublisher publisher, LogsViewModel logsViewModel)
    {
        Logs = logsViewModel;

        ToggleProducingCommand = new RelayCommand(() => IsProducing = !IsProducing);
        ScaleOutCommand = new AsyncRelayCommand(() => publisher.ExecuteCommandAsync(new ScaleOutCommand()));
        ScaleInCommand = new AsyncRelayCommand(() => publisher.ExecuteCommandAsync(new ScaleInCommand()));
        OpenLogsInVsCodeCommand = new RelayCommand(() => ProcessHelper.Start("code", FileSystemHelper.LogsFolder));

        InitTopics();
        _topicsDictionary = Topics.Where(topic => topic is not OverallTopicViewModel).ToDictionary(topic => topic.TopicName);

        // Add the overall stats as first topic to be displayed in the same grid
        OverallTopicViewModel overallTopic = new();
        TopicsPlusOverall.Add(overallTopic);
        OverallMessagesStatistics = overallTopic.Statistics;

        foreach (TopicViewModel topic in Topics)
        {
            TopicsPlusOverall.Add(topic);
        }
    }

    public ICommand ToggleProducingCommand { get; }

    public ICommand ScaleOutCommand { get; }

    public ICommand ScaleInCommand { get; }

    public ICommand OpenLogsInVsCodeCommand { get; }

    public TopicStatisticsViewModel OverallMessagesStatistics { get; }

    public ObservableCollection<TopicViewModel> TopicsPlusOverall { get; } = [];

    public ObservableCollection<TopicViewModel> Topics { get; } = [];

    public IEnumerable<KafkaTopicViewModel> KafkaTopics => Topics.OfType<KafkaTopicViewModel>();

    public IEnumerable<MqttTopicViewModel> MqttTopics => Topics.OfType<MqttTopicViewModel>();

    public bool IsProducing
    {
        get => _isProducing;
        set
        {
            SetProperty(ref _isProducing, value, nameof(IsProducing));

            if (value && ProduceSpeedMultiplier == 0)
                ProduceSpeedMultiplier = 1;
            else if (!value && ProduceSpeedMultiplier > 0)
                ProduceSpeedMultiplier = 0;
        }
    }

    public double ProduceSpeedMultiplier
    {
        get => _produceSpeedMultiplier;
        set
        {
            SetProperty(ref _produceSpeedMultiplier, value, nameof(ProduceSpeedMultiplier));

            if (value > 0 && !IsProducing)
                IsProducing = true;
            else if (value == 0 && IsProducing)
                IsProducing = false;
        }
    }

    public ObservableCollection<ContainerInstanceViewModel> ContainerInstances { get; } = [];

    public AutoScalingViewModel AutoScaling { get; } = new();

    public LogsViewModel Logs { get; }

    public TopicViewModel GetTopic(string name) => _topicsDictionary[name];

    public bool TryGetTopic(string name, [NotNullWhen(true)] out TopicViewModel? topic) => _topicsDictionary.TryGetValue(name, out topic);

    private void InitTopics()
    {
        Topics.Add(
            new KafkaTopicViewModel(
                TopicNames.Kafka.Single,
                8,
                TimeSpan.FromMilliseconds(200),
                0.01));
        Topics.Add(
            new KafkaTopicViewModel(
                TopicNames.Kafka.Batch,
                12,
                TimeSpan.FromMilliseconds(50),
                0.01));
        Topics.Add(
            new KafkaTopicViewModel(
                TopicNames.Kafka.Unbounded,
                8,
                TimeSpan.FromMilliseconds(100),
                0.01));

        Topics.Add(
            new MqttTopicViewModel(
                TopicNames.Mqtt.Single,
                TimeSpan.FromMilliseconds(200),
                0.01));
        Topics.Add(
            new MqttTopicViewModel(
                TopicNames.Mqtt.Unbounded,
                TimeSpan.FromMilliseconds(50),
                0.01));
    }
}
