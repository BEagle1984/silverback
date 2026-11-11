// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Silverback.Tests.Integration.E2E.Util;

// The batch timeout can't be controlled: the messages are produced at the times making the batch refuse the third message, for a batch
// with a max wait time of 2 seconds
public static class BatchTimeoutTiming
{
    public static async Task ProduceWhileFirstBatchTimesOutAsync(
        Func<Task> produceFirstAsync,
        Func<Task> produceSecondAsync,
        Func<Task> produceThirdAsync,
        Task firstMessageReceived)
    {
        // The timer completing the batch on timeout is started when the first message is added and ticks every second
        await produceFirstAsync();
        await firstMessageReceived.WaitAsync(TimeSpan.FromSeconds(10));
        Stopwatch stopwatch = Stopwatch.StartNew();

        // The batch times out 2 seconds after the second message is added (at about 2300 ms), but the timer completes it only at its
        // third tick (at about 3000 ms): the third message is produced in between, so that the batch refuses it
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        await produceSecondAsync();

        TimeSpan remaining = TimeSpan.FromMilliseconds(2650) - stopwatch.Elapsed;

        if (remaining > TimeSpan.Zero)
            await Task.Delay(remaining);

        await produceThirdAsync();
    }
}
