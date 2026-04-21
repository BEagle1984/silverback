// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Concurrency;

// Concurrency tests for Core and Integration patterns not covered by the
// SequenceBase/MessageStreamProvider/SequenceStore tests.
public class CoreConcurrencyPatternCoyoteTests
{
    private readonly ITestOutputHelper _output;

    public CoreConcurrencyPatternCoyoteTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // #1 — LazyMessageStreamEnumerable TaskCompletionSource double-set.
    //
    // LazyMessageStreamEnumerable`1.cs:45-59:
    //   GetOrCreateStream() calls _taskCompletionSource.SetResult(stream)
    //   Cancel() calls _taskCompletionSource.SetCanceled()
    //
    // No synchronization. If a subscriber triggers GetOrCreateStream while
    // the sequence abort path triggers Cancel, the TCS gets set twice.
    // InvalidOperationException from the second Set is thrown in a
    // fire-and-forget context, silently lost.
    [Fact]
    public void LazyStream_GetOrCreateRacingCancel_ShouldNotDoubleSetTcs()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                TaskCompletionSource<object> tcs = new();
                object? createdStream = null;

                // Mirrors GetOrCreateStream: check null, create, SetResult
                Task create = Task.Run(
                    () =>
                    {
                        if (createdStream == null)
                        {
                            createdStream = new object();
                            tcs.SetResult(createdStream);
                        }
                    });

                // Mirrors Cancel: SetCanceled
                Task cancel = Task.Run(
                    () =>
                    {
                        tcs.SetCanceled();
                    });

                // At least one must throw if both try to set.
                // In production this exception is swallowed. Here we detect it.
                bool exceptionObserved = false;
                try
                {
                    await Task.WhenAll(create, cancel).ConfigureAwait(false);
                }
                catch (InvalidOperationException)
                {
                    exceptionObserved = true;
                }
                catch (TaskCanceledException)
                {
                    // SetCanceled won the race; that's one valid outcome
                }

                // Invariant: if no exception, exactly one of SetResult or SetCanceled
                // should have succeeded. The TCS should be in a terminal state.
                // The bug is that both CAN succeed in the absence of synchronization,
                // which means the second call throws. In production that exception
                // is lost (fire-and-forget), causing silent message loss.
                if (exceptionObserved)
                {
                    // The race was caught: double-set occurred. This is the bug.
                    true.ShouldBeFalse(
                        "TaskCompletionSource was set twice (SetResult + SetCanceled raced). " +
                        "LazyMessageStreamEnumerable.GetOrCreateStream and Cancel have no " +
                        "synchronization; the second Set throws InvalidOperationException " +
                        "which is silently lost in a fire-and-forget context. See finding #1.");
                }
            },
            _output);
    }

    // #2 — ConsumerChannel CancellationTokenSource dispose-while-active.
    //
    // ConsumerChannel`1.cs:86-102:
    //   StartReading() disposes _readCancellationTokenSource and creates a new one
    //   if the old one is canceled. No lock. Concurrent readers that cached
    //   ReadCancellationToken from the old source get ObjectDisposedException.
    [Fact]
    public void ConsumerChannel_StartReadingDisposingCts_ShouldNotThrow()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                CancellationTokenSource cts = new();
                CancellationToken cachedToken = cts.Token;
                bool objectDisposedObserved = false;

                // T1: simulates StartReading — cancel then dispose+recreate
                Task startReading = Task.Run(
                    async () =>
                    {
                        await cts.CancelAsync().ConfigureAwait(false);
                        cts.Dispose();
                        cts = new CancellationTokenSource();
                    });

                // T2: simulates a reader that cached the old token
                Task reader = Task.Run(
                    () =>
                    {
                        try
                        {
                            // Access the token's properties — this throws if
                            // the source was disposed
                            _ = cachedToken.IsCancellationRequested;
                            cachedToken.ThrowIfCancellationRequested();
                        }
                        catch (ObjectDisposedException)
                        {
                            objectDisposedObserved = true;
                        }
                    });

                await Task.WhenAll(startReading, reader).ConfigureAwait(false);

                objectDisposedObserved.ShouldBeFalse(
                    "Reader observed ObjectDisposedException on a cached CancellationToken " +
                    "after StartReading disposed and recreated the CancellationTokenSource. " +
                    "ConsumerChannel.StartReading has no synchronization around the " +
                    "dispose+recreate; concurrent readers that hold the old token crash. " +
                    "See finding #2.");
            },
            _output);
    }

    // #4 — InMemoryKafkaOffsetStore: live collection escapes lock scope.
    //
    // InMemoryKafkaOffsetStore.cs:20-28:
    //   GetStoredOffsets returns offsetsByTopicPartition.Values — a LIVE view
    //   into the Dictionary. The lock is released before the caller enumerates.
    //   StoreOffsetsAsync mutates the dictionary under lock, but the returned
    //   Values collection sees the mutation without any lock protection.
    [Fact]
    public void InMemoryKafkaOffsetStore_EnumerateReturnedCollectionWhileStoring_ShouldNotThrow()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                // Model the pattern: Dictionary + lock on outer dict, return inner.Values
                Dictionary<string, Dictionary<int, string>> store = new();
                object lockObj = new();

                // Seed
                lock (lockObj)
                {
                    store["group1"] = new Dictionary<int, string> { [0] = "offset-0" };
                }

                // GetStoredOffsets: returns live .Values inside the lock, caller
                // enumerates OUTSIDE the lock
                ICollection<string>? liveValues = null;
                lock (lockObj)
                {
                    if (store.TryGetValue("group1", out var inner))
                        liveValues = inner.Values; // live view, not a snapshot
                }

                // T1: enumerate the live Values collection (outside the lock)
                Task enumerate = Task.Run(
                    async () =>
                    {
                        if (liveValues == null) return;
                        using IEnumerator<string> enumerator = liveValues.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            await Task.Yield();
                        }
                    });

                // T2: store new offsets (mutates the inner dict under lock)
                Task storeOffsets = Task.Run(
                    () =>
                    {
                        lock (lockObj)
                        {
                            store["group1"]![1] = "offset-1";
                            store["group1"]![2] = "offset-2";
                        }
                    });

                await Task.WhenAll(enumerate, storeOffsets).ConfigureAwait(false);
            },
            _output);
    }

    // #5 — EnumerableSelectExtensions.ParallelSelectAsync semaphore corruption.
    //
    // EnumerableSelectExtensions.cs:42-61:
    //   semaphore.Release() is in the try block (not finally). If the selector
    //   throws, the catch cancels the CTS but does NOT release the semaphore.
    //   The semaphore token is leaked. Other tasks waiting on WaitAsync block
    //   until the CTS cancellation propagates, but the semaphore count is
    //   permanently reduced.
    //
    //   Additionally, if WaitAsync is canceled (by the CTS from another task's
    //   exception), the OperationCanceledException propagates but the semaphore
    //   was never acquired, so there's no leak in THAT path. The leak is
    //   specifically: task acquires semaphore, selector throws, catch does NOT
    //   release.
    [Fact]
    public void ParallelSelect_SelectorThrows_ShouldNotLeakSemaphoreToken()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                const int maxDoP = 2;
                SemaphoreSlim semaphore = new(maxDoP);
                using CancellationTokenSource cts = new();
                int completed = 0;
                bool leaked = false;

                async Task<int> InvokeSelector(int value)
                {
                    await semaphore.WaitAsync(cts.Token).ConfigureAwait(false);
                    try
                    {
                        // Mirrors the production pattern: Release in try, not finally
                        if (value == 3)
                            throw new InvalidOperationException("selector failure");

                        await Task.Yield();
                        Interlocked.Increment(ref completed);
                        semaphore.Release();
                        return value;
                    }
                    catch (Exception)
                    {
                        await cts.CancelAsync().ConfigureAwait(false);
                        throw;
                        // NOTE: semaphore is NOT released here — this is the bug
                    }
                }

                try
                {
                    Task[] tasks = new[] { 1, 2, 3, 4, 5 }
                        .Select(i => Task.Run(async () => await InvokeSelector(i).ConfigureAwait(false)))
                        .ToArray();
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch
                {
                    // Expected: one selector threw
                }

                // After all tasks settle, the semaphore count should equal maxDoP
                // if all tokens were properly released. A leaked token means
                // count < maxDoP.
                if (semaphore.CurrentCount < maxDoP)
                    leaked = true;

                leaked.ShouldBeFalse(
                    $"Semaphore count is {semaphore.CurrentCount}, expected {maxDoP}. " +
                    "ParallelSelectAsync releases the semaphore in the try block, not " +
                    "the finally. When the selector throws, the token is leaked. " +
                    "See finding #5.");
            },
            _output);
    }
}
