// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static class EnumerableDisposeAllExtensions
{
    public static async ValueTask DisposeAllAsync(this IEnumerable enumerable)
    {
        foreach (object obj in enumerable)
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
    }
}
