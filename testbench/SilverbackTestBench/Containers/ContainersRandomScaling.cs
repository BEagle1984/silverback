// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using Silverback.TestBench.Configuration;

namespace Silverback.TestBench.Containers;

public sealed class ContainersRandomScaling : IDisposable
{
    private readonly ContainersOrchestrator _containersOrchestrator;

    private Timer? _timer;

    public ContainersRandomScaling(ContainersOrchestrator containersOrchestrator)
    {
        _containersOrchestrator = containersOrchestrator;
    }

    public bool IsEnabled => _timer != null;

    public TimeSpan Interval { get; private set; } = TimeSpan.FromMinutes(2);

    public double Chance { get; private set; } = 1;

    public int MinInstances { get; private set; } = 2;

    public int MaxInstances { get; private set; } = 5;

    public void Enable()
    {
        if (_timer != null)
            return;

        _timer = new Timer(_ => SetRandomInstances(), null, Interval, Interval);
    }

    public void Disable()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public void Setup(TimeSpan interval, double chance, int minInstances, int maxInstances)
    {
        Interval = interval;
        Chance = chance;
        MinInstances = minInstances;
        MaxInstances = maxInstances;
    }

    public void Dispose() => Disable();

    private void SetRandomInstances()
    {
        if (Random.Shared.Next(0, 1) >= Chance)
            return;

        _containersOrchestrator.SetInstances(DockerContainers.Consumer, Random.Shared.Next(MinInstances, MaxInstances + 1));
    }
}
