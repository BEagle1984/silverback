// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;

namespace Silverback.Tests.Extended.Benchmarks.Common;

public static class BenchmarkActivity
{
    /// <summary>
    ///     Starts an activity (which becomes the current one) carrying three baggage items, as the ones propagated to the message headers.
    /// </summary>
    /// <returns>
    ///     The started activity, to be disposed by the caller.
    /// </returns>
    public static Activity StartWithBaggage()
    {
        Activity activity = new("benchmark");
        activity.AddBaggage("tenant", "benchmark-tenant");
        activity.AddBaggage("user", "benchmark-user");
        activity.AddBaggage("correlation", "0f8fad5bd9cb469fa16570867728950e");
        return activity.Start();
    }
}
