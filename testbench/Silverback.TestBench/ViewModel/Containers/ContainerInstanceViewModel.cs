// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Ductus.FluentDocker.Services;
using Microsoft.Extensions.Logging;
using Silverback.TestBench.Containers;
using Silverback.TestBench.Producer;
using Silverback.TestBench.ViewModel.Framework;
using Silverback.TestBench.ViewModel.Logs;
using Silverback.TestBench.ViewModel.Trace;

namespace Silverback.TestBench.ViewModel.Containers;

public class ContainerInstanceViewModel : ViewModelBase
{
    private DateTime? _started;

    private DateTime? _stopped;

    private ContainerStatus _status = ContainerStatus.Starting;

    public ContainerInstanceViewModel(
        IContainerService containerService,
        MessagesTracker messagesTracker,
        LogsViewModel logsViewModel,
        TraceViewModel traceViewModel,
        ILoggerFactory loggerFactory)
    {
        ContainerService = containerService;
        LogParser = new ContainerLogParser(
            this,
            logsViewModel,
            traceViewModel,
            messagesTracker,
            loggerFactory.CreateLogger<ContainerLogParser>());
    }

    public IContainerService ContainerService { get; }

    public ContainerLogParser LogParser { get; }

    public ContainerStatisticsViewModel Statistics { get; } = new();

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

    public void SetStopping() => Status = ContainerStatus.Stopping;

    public void SetStopped(DateTime stopped)
    {
        Stopped = stopped;
        Status = ContainerStatus.Stopped;
    }
}
