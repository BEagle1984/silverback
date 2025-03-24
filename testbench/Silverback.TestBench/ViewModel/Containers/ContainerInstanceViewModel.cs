// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Confluent.Kafka;
using Ductus.FluentDocker.Services;
using Microsoft.Extensions.Logging;
using Silverback.TestBench.Containers;
using Silverback.TestBench.Producer;
using Silverback.TestBench.ViewModel.Framework;

namespace Silverback.TestBench.ViewModel.Containers;

public class ContainerInstanceViewModel : ViewModelBase
{
    private readonly ILogger<ContainerInstanceViewModel> _logger;

    private DateTime? _started;

    private DateTime? _stopped;

    private ContainerStatus _status = ContainerStatus.Starting;

    public ContainerInstanceViewModel(
        IContainerService containerService,
        MessagesTracker messagesTracker,
        MainViewModel mainViewModel,
        ILoggerFactory loggerFactory)
    {
        ContainerService = containerService;
        LogParser = new ContainerLogParser(
            this,
            mainViewModel,
            messagesTracker,
            loggerFactory.CreateLogger<ContainerLogParser>());

        _logger = loggerFactory.CreateLogger<ContainerInstanceViewModel>();

        StopCommand = new AsyncRelayCommand(
            () => Task.Run(Stop),
            () => Status == ContainerStatus.Running);
    }

    public ICommand StopCommand { get; }

    public IContainerService ContainerService { get; }

    public ContainerLogParser LogParser { get; }

    public ContainerStatisticsViewModel Statistics { get; } = new();

    public ObservableCollection<TopicPartition> AssignedKafkaPartitions { get; } = [];

    public ObservableCollection<string> SubscribedMqttTopics { get; } = [];

    public DateTime? Started
    {
        get => _started;
        private set => SetProperty(ref _started, value, nameof(Started));
    }

    public DateTime? Stopped
    {
        get => _stopped;
        private set => SetProperty(ref _stopped, value, nameof(Stopped));
    }

    public ContainerStatus Status
    {
        get => _status;
        private set => SetProperty(ref _status, value, nameof(Status));
    }

    public void SetStarted(DateTime started)
    {
        Started = started;
        Status = ContainerStatus.Running;
    }

    public void Stop()
    {
        Status = ContainerStatus.Stopping;
        ContainerService.Dispose();

        _logger.LogInformation("Stopped container {ContainerName}", ContainerService.Name);
    }

    public void SetStopped(DateTime stopped)
    {
        Stopped = stopped;
        Status = ContainerStatus.Stopped;
    }
}
