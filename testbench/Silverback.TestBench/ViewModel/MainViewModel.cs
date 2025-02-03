// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using Silverback.TestBench.ViewModel.Trace;

namespace Silverback.TestBench.ViewModel;

public class MainViewModel : ViewModelBase
{
    private readonly Dictionary<string, TopicViewModel> _topicsDictionary;

    private ContainerInstanceViewModel? _selectedContainerInstance;

    private bool _isProducing;

    private double _produceSpeedMultiplier;

    public MainViewModel(IPublisher publisher, LogsViewModel logsViewModel, TraceViewModel traceViewModel)
    {
        Logs = logsViewModel;
        Trace = traceViewModel;

        ToggleProducingCommand = new RelayCommand(() => IsProducing = !IsProducing);
        ScaleOutCommand = new AsyncRelayCommand(() => publisher.ExecuteCommandAsync(new ScaleOutCommand()));
        ScaleInCommand = new AsyncRelayCommand(() => publisher.ExecuteCommandAsync(new ScaleInCommand()));
        OpenLogsInVsCodeCommand = new RelayCommand(() => ProcessHelper.Start("code", FileSystemHelper.LogsFolder));

        InitTopics();
        _topicsDictionary = Topics.Where(topic => topic is not OverallTopicViewModel).ToDictionary(topic => topic.TopicName);

        // Wrap overall statistics in a collection to allow binding to a data grid
        OverallTopicViewModel overallTopic = new();
        OverallTopicsStatistics = [overallTopic];
        OverallMessagesStatistics = overallTopic.Statistics;

        // Keep log viewer filters in sync with the container instances
        ContainerInstances.CollectionChanged += (_, args) =>
        {
            if (args.Action != NotifyCollectionChangedAction.Add)
                return;

            foreach (ContainerInstanceViewModel container in args.NewItems!)
            {
                logsViewModel.ContainerFilterValues.Add(container.ContainerService.Name);
            }
        };
    }

    public ICommand ToggleProducingCommand { get; }

    public ICommand ScaleOutCommand { get; }

    public ICommand ScaleInCommand { get; }

    public ICommand OpenLogsInVsCodeCommand { get; }

    public TopicStatisticsViewModel OverallMessagesStatistics { get; }

    public ObservableCollection<TopicViewModel> Topics { get; } = [];

    public IEnumerable<OverallTopicViewModel> OverallTopicsStatistics { get; }

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

    public ContainerInstanceViewModel? SelectedContainerInstance
    {
        get => _selectedContainerInstance;
        set => SetProperty(ref _selectedContainerInstance, value, nameof(SelectedContainerInstance));
    }

    public AutoScalingViewModel AutoScaling { get; } = new();

    public LogsViewModel Logs { get; }

    public TraceViewModel Trace { get; }

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
