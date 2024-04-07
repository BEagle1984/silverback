// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;

namespace Silverback.Tests;

public static class CountdownEventExtensions
{
    public static void WaitOrThrow(this CountdownEvent countdownEvent, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(5);

        if (!countdownEvent.Wait(timeout.Value))
            throw new TimeoutException("Timeout waiting for the event to be set.");
    }
}
