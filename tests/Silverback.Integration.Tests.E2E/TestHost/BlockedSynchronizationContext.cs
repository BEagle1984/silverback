// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Tests.Integration.E2E.TestHost;

// Emulates the synchronization context of a UI thread blocked waiting for a synchronous call: the posted callbacks never run, so
// any continuation resuming on the captured context deadlocks
public sealed class BlockedSynchronizationContext : SynchronizationContext
{
    public static Task RunAsync(Action action) =>
        Task.Factory.StartNew(
            () =>
            {
                SetSynchronizationContext(new BlockedSynchronizationContext());
                action();
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

    public override void Post(SendOrPostCallback d, object? state)
    {
        // Never run, since the owning thread is blocked
    }

    public override void Send(SendOrPostCallback d, object? state) => throw new NotSupportedException();
}
