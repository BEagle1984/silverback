// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Tests;

public class ParallelTestingUtil
{
    private int _lastStep;

    public ConcurrentBag<int> Steps { get; } = new();

    public void DoWork()
    {
        Thread.Sleep(20);
        Steps.Add(_lastStep + 1);
        Thread.Sleep(20);
        Interlocked.Increment(ref _lastStep);
    }

    public async Task DoWorkAsync()
    {
        await Task.Delay(20);
        Steps.Add(_lastStep + 1);
        await Task.Delay(20);
        Interlocked.Increment(ref _lastStep);
    }
}
