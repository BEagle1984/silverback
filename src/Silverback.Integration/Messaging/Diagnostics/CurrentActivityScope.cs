// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;

namespace Silverback.Messaging.Diagnostics;

// Temporarily sets the current activity, so that the logs written from a thread the activity didn't flow to (e.g. the one delivering the
// broker acknowledgment) are still correlated with it. The activity must not be stopped yet, otherwise it cannot be set as current.
internal readonly struct CurrentActivityScope : IDisposable
{
    private readonly Activity? _previousActivity;

    private readonly bool _isSwapped;

    private CurrentActivityScope(Activity? previousActivity)
    {
        _previousActivity = previousActivity;
        _isSwapped = true;
    }

    public static CurrentActivityScope Enter(Activity? activity)
    {
        Activity? previousActivity = Activity.Current;

        if (activity == null || activity == previousActivity)
            return default;

        Activity.Current = activity;
        return new CurrentActivityScope(previousActivity);
    }

    public void Dispose()
    {
        if (_isSwapped)
            ActivitySources.RestoreCurrent(_previousActivity);
    }
}
