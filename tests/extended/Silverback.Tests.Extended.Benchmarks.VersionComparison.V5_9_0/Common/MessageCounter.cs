// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Tests.Extended.Benchmarks.VersionComparison.V5_9_0.Common;

/// <summary>
///     Lets a benchmark wait until the subscriber has received the expected number of messages.
/// </summary>
public sealed class MessageCounter
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

    private readonly System.Threading.Lock _syncLock = new();

    private TaskCompletionSource _completionSource = new();

    private int _remaining;

    public void Reset(int expectedCount)
    {
        lock (_syncLock)
        {
            _remaining = expectedCount;
            _completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    public void Increment()
    {
        lock (_syncLock)
        {
            if (--_remaining == 0)
                _completionSource.TrySetResult();
        }
    }

    public Task WaitAsync()
    {
        lock (_syncLock)
        {
            return _completionSource.Task.WaitAsync(Timeout);
        }
    }
}
