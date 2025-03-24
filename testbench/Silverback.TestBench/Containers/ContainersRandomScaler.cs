// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Silverback.Messaging.Publishing;
using Silverback.TestBench.Containers.Commands;
using Silverback.TestBench.ViewModel;
using Silverback.TestBench.ViewModel.Containers;

namespace Silverback.TestBench.Containers;

public sealed class ContainersRandomScaler : IHostedService, IDisposable
{
    private readonly IPublisher _publisher;

    private readonly AutoScalingViewModel _viewModel;

    private Timer? _timer;

    public ContainersRandomScaler(MainViewModel mainViewModel, IPublisher publisher)
    {
        _publisher = publisher;
        _viewModel = mainViewModel.AutoScaling;

        _viewModel.PropertyChanged += (_, _) => SetTimer();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        SetTimer();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_timer == null)
            return;

        await _timer.DisposeAsync();
        _timer = null;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void SetTimer()
    {
        if (_timer == null && _viewModel.IsEnabled)
        {
            _timer = new Timer(_ => SetRandomInstances(), null, _viewModel.Interval, _viewModel.Interval);
        }
        else if (_timer != null && !_viewModel.IsEnabled)
        {
            _timer.Dispose();
            _timer = null;
        }
        else
        {
            _timer?.Change(_viewModel.Interval, _viewModel.Interval);
        }
    }

    private void SetRandomInstances()
    {
        if (Random.Shared.Next(0, 1) >= _viewModel.Chance)
            return;

        _publisher.ExecuteCommand(new SetInstancesCommand(Random.Shared.Next(_viewModel.MinInstances, _viewModel.MaxInstances + 1)));
    }
}
