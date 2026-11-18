// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static class EnumerableDisposeAllExtensions
{
    // A failing object doesn't prevent the others from being disposed: the exceptions are rethrown at the end
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Rethrown")]
    public static void DisposeAll(this IEnumerable enumerable)
    {
        List<Exception>? exceptions = null;

        foreach (object obj in enumerable)
        {
            try
            {
                switch (obj)
                {
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                    case IAsyncDisposable asyncDisposable:
                        asyncDisposable.DisposeAsync().SafeWait();
                        break;
                }
            }
            catch (Exception ex)
            {
                (exceptions ??= []).Add(ex);
            }
        }

        ThrowIfAny(exceptions);
    }

    // A failing object doesn't prevent the others from being disposed: the exceptions are rethrown at the end
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Rethrown")]
    public static async ValueTask DisposeAllAsync(this IEnumerable enumerable)
    {
        List<Exception>? exceptions = null;

        foreach (object obj in enumerable)
        {
            try
            {
                switch (obj)
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }
            catch (Exception ex)
            {
                (exceptions ??= []).Add(ex);
            }
        }

        ThrowIfAny(exceptions);
    }

    private static void ThrowIfAny(List<Exception>? exceptions)
    {
        if (exceptions == null)
            return;

        if (exceptions.Count == 1)
            ExceptionDispatchInfo.Throw(exceptions[0]);

        throw new AggregateException(exceptions);
    }
}
