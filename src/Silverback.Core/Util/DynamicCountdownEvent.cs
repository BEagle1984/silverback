// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Util;

internal sealed class DynamicCountdownEvent : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(0);

    private int _currentCount;

    public int CurrentCount => Volatile.Read(ref _currentCount);

    public void Reset(int count = 0)
    {
        Check.GreaterOrEqualTo(count, nameof(count), 0);

        _currentCount = count;
    }

    public void AddCount(int count = 1)
    {
        Check.GreaterOrEqualTo(count, nameof(count), 1);

        Interlocked.Add(ref _currentCount, count);
    }

    public void Signal(int count = 1)
    {
        Check.GreaterOrEqualTo(count, nameof(count), 1);

        if (Interlocked.Add(ref _currentCount, -count) <= 0)
        {
            _semaphore.Release();
        }
    }

    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        while (Volatile.Read(ref _currentCount) > 0)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose() => _semaphore.Dispose();
}
