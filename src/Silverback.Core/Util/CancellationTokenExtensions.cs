// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static class CancellationTokenExtensions
{
    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Named after ValueTask.AsTask")]
    public static Task AsTask(this CancellationToken cancellationToken)
    {
        TaskCompletionSource<bool> taskCompletionSource = new();
        cancellationToken.Register(tcs => ((TaskCompletionSource<bool>)tcs!).SetResult(true), taskCompletionSource);
        return taskCompletionSource.Task;
    }
}
