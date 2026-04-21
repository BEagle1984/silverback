// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Concurrency;

// Audit finding C3 — ConsumerChannelsManager releases _messagesLimiterSemaphoreSlim based
// on CurrentCount, not ownership.
//
// src/Silverback.Integration.Kafka/Messaging/Broker/Kafka/ConsumerChannelsManager.cs:150-165:
//
//   if (_channels.Count > _consumer.Configuration.MaxDegreeOfParallelism)
//       await _messagesLimiterSemaphoreSlim.WaitAsync(...);  // conditional acquire
//   try { await _consumer.HandleMessageAsync(...); }
//   finally
//   {
//       if (_messagesLimiterSemaphoreSlim.CurrentCount < _consumer.Configuration.MaxDegreeOfParallelism)
//           _messagesLimiterSemaphoreSlim.Release();          // condition != "did I acquire"
//   }
//
// The interleaving that triggers the bug:
//   1. T1, T2: acquire tokens (channelCount > maxDoP). Semaphore count = 0.
//   2. Partition revoke: channelCount drops to <= maxDoP.
//   3. T3: enters with channelCount <= maxDoP, skips acquire. Finishes. Finally sees
//      CurrentCount=0 < maxDoP → releases a token it never owned. Count = 1.
//   4. Partition reassignment: channelCount goes back above maxDoP.
//   5. T4: acquires the extra token. Now T1, T2, T4 are all active simultaneously.
//      Active count = 3 > maxDoP = 2. The limiter failed to limit.
//
// Over time, after many revoke/assign cycles, the leaked tokens accumulate in the
// opposite direction too (under-release), eventually starving all message processing.
// This matches the reported bug: "stops consuming after several thousand messages"
// with 24 partitions.
//
// The test tracks peak concurrent active handlers and asserts it never exceeds maxDoP.
//
// Fix direction: capture `bool acquired` locally and release iff true.
public class ConsumerChannelsSemaphoreCoyoteTests
{
    private readonly ITestOutputHelper _output;

    public ConsumerChannelsSemaphoreCoyoteTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SemaphoreCurrentCountRelease_ShouldNotExceedMaxDoP_AfterChannelCountDrop()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                const int maxDoP = 2;
                SemaphoreSlim limiter = new(maxDoP, maxDoP);
                int channelCount = 4;

                int active = 0;
                int maxObservedActive = 0;

                // Gate: holds T1/T2 mid-processing so they occupy their semaphore tokens
                // while T3 enters, exits, and over-releases.
                SemaphoreSlim gate = new(0, 4);

                async Task ProcessHeld()
                {
                    if (channelCount > maxDoP)
                        await limiter.WaitAsync().ConfigureAwait(false);

                    try
                    {
                        InterlockedMax(ref maxObservedActive, Interlocked.Increment(ref active));
                        await gate.WaitAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref active);
                        if (limiter.CurrentCount < maxDoP)
                            limiter.Release();
                    }
                }

                async Task ProcessQuick()
                {
                    if (channelCount > maxDoP)
                        await limiter.WaitAsync().ConfigureAwait(false);

                    try
                    {
                        InterlockedMax(ref maxObservedActive, Interlocked.Increment(ref active));
                        await Task.Yield();
                    }
                    finally
                    {
                        Interlocked.Decrement(ref active);
                        if (limiter.CurrentCount < maxDoP)
                            limiter.Release();
                    }
                }

                // Phase 1: T1, T2 acquire tokens and hold via gate.
                // Semaphore count: 2 → 1 → 0. active = 2.
                Task t1 = Task.Run(ProcessHeld);
                Task t2 = Task.Run(ProcessHeld);
                await Task.Yield();
                await Task.Yield();

                // Partition revoke: channels drop to <= maxDoP.
                channelCount = 2;

                // T3: enters, skips acquire (2 > 2 = false). Finishes quickly.
                // T3 finally: CurrentCount = 0 < 2 → releases a token it never owned!
                // Count: 0 → 1. active briefly 3 (T1 + T2 + T3), then back to 2.
                Task t3 = Task.Run(ProcessQuick);
                await t3.ConfigureAwait(false);

                // Partition reassignment: channels go back up.
                channelCount = 4;

                // T4: acquires the over-released token. Count: 1 → 0.
                // Now T1, T2, T4 are all active inside the try block.
                // active = 3, which exceeds maxDoP = 2.
                Task t4 = Task.Run(ProcessHeld);
                await Task.Yield();
                await Task.Yield();

                // Release everyone
                gate.Release(4);
                await Task.WhenAll(t1, t2, t4).ConfigureAwait(false);

                // Invariant: the semaphore should have prevented more than maxDoP
                // concurrent active handlers at any point.
                maxObservedActive.ShouldBeLessThanOrEqualTo(
                    maxDoP,
                    $"Peak concurrent active handlers was {maxObservedActive}, exceeding maxDoP={maxDoP}. " +
                    "The CurrentCount-based release in ConsumerChannelsManager over-released a token " +
                    "that was never acquired (after a channel-count drop), allowing more concurrent " +
                    "processing than the limiter should permit. See audit C3.");
            },
            _output);
    }

    private static void InterlockedMax(ref int location, int value)
    {
        int current;
        do
        {
            current = Volatile.Read(ref location);
        }
        while (value > current && Interlocked.CompareExchange(ref location, value, current) != current);
    }
}
