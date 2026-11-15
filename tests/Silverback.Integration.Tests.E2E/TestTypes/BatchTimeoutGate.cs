// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Tests.Integration.E2E.TestTypes.Logging;

namespace Silverback.Tests.Integration.E2E.TestTypes;

// Holds the handling of the elapsed max wait time of the first batch (see GatedBatchSequenceReader), so that the next message is
// deterministically refused by the batch whose max wait time elapsed
public sealed class BatchTimeoutGate
{
    private readonly TaskCompletionSource _timeoutElapsed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int _isHeld;

    // Waits until the max wait time of the first batch elapsed, produces the next message and releases the timeout once the batch
    // refused the message
    public async Task ProduceRefusedMessageAsync(Func<Task> produceAsync, LogEventsCollector logEventsCollector)
    {
        try
        {
            await _timeoutElapsed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await produceAsync();

            await AsyncTestingUtil.WaitAsync(() => logEventsCollector.ContainsSequenceAddCanceled(nameof(GatedBatchSequence)));
            logEventsCollector.ContainsSequenceAddCanceled(nameof(GatedBatchSequence)).ShouldBeTrue();
        }
        finally
        {
            // Release the timeout also when failing, not to leave the batch pending
            _released.TrySetResult();
        }
    }

    public Task HoldFirstTimeoutAsync()
    {
        if (Interlocked.Exchange(ref _isHeld, 1) != 0)
            return Task.CompletedTask;

        _timeoutElapsed.TrySetResult();
        return _released.Task;
    }
}
