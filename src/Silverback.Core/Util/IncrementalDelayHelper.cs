// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Util;

internal static class IncrementalDelayHelper
{
    public static TimeSpan Compute(int failedAttempts, TimeSpan initialDelay, TimeSpan delayIncrement, double delayFactor, TimeSpan? maxDelay)
    {
        TimeSpan delay = (initialDelay + (failedAttempts * delayIncrement)) * Math.Pow(delayFactor, failedAttempts);
        return delay > maxDelay ? maxDelay.Value : delay;
    }
}
