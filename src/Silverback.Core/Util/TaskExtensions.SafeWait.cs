// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static partial class TaskExtensions
{
    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Reviewed")]
    public static void SafeWait(this Task task)
    {
        if (task.IsCompletedSuccessfully)
            return;

        task.GetAwaiter().GetResult();
    }

    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Reviewed")]
    public static TResult SafeWait<TResult>(this Task<TResult> task)
    {
        if (task.IsCompletedSuccessfully)
            return task.Result;

        return task.GetAwaiter().GetResult();
    }

    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Reviewed")]
    public static void SafeWait(this ValueTask valueTask)
    {
        if (valueTask.IsCompletedSuccessfully)
            return;

        valueTask.AsTask().GetAwaiter().GetResult();
    }

    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Reviewed")]
    public static TResult SafeWait<TResult>(this ValueTask<TResult> valueTask)
    {
        if (valueTask.IsCompletedSuccessfully)
            return valueTask.GetAwaiter().GetResult();

        return valueTask.AsTask().GetAwaiter().GetResult();
    }
}
