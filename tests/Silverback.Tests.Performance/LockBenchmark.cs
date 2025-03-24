// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace Silverback.Tests.Performance;

[SimpleJob]
[MemoryDiagnoser]
public class LockBenchmark
{
    private readonly object _syncLock = new();

    private SpinLock _spinLock;

    [Benchmark(Baseline = true)]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Benchmark")]
    public void NoLock()
    {
        for (int i = 0; i < 1000; i++)
        {
            _ = i;
        }
    }

    [Benchmark]
    public void Lock()
    {
        lock (_syncLock)
        {
            for (int i = 0; i < 1000; i++)
            {
                _ = i;
            }
        }
    }

    [Benchmark]
    public void SpinLock()
    {
        bool lockTaken = false;

        try
        {
            _spinLock.Enter(ref lockTaken);
            for (int i = 0; i < 1000; i++)
            {
                _ = i;
            }
        }
        finally
        {
            if (lockTaken)
                _spinLock.Exit();
        }
    }
}
