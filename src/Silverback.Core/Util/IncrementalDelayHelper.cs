// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Util;

internal static class IncrementalDelayHelper
{
    // The failed attempts include the one being retried: the first retry (1 failed attempt) is delayed by the initial delay, and the
    // increment and the factor are applied once per subsequent retry
    public static TimeSpan Compute(int failedAttempts, TimeSpan initialDelay, TimeSpan delayIncrement, double delayFactor, TimeSpan? maxDelay)
    {
        int previousRetries = Math.Max(failedAttempts - 1, 0);
        TimeSpan delay = (initialDelay + (previousRetries * delayIncrement)) * Math.Pow(delayFactor, previousRetries);
        return delay > maxDelay ? maxDelay.Value : delay;
    }
}
