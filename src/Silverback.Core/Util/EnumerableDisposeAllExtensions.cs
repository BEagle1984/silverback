// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Threading.Tasks;

namespace Silverback.Util;

// TODO: Test
internal static class EnumerableDisposeAllExtensions
{
    public static async ValueTask DisposeAllAsync(this IEnumerable enumerable)
    {
        foreach (object obj in enumerable)
        {
            if (obj is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                continue;
            }

            if (obj is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
