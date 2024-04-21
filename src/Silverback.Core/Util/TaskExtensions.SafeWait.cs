// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static partial class TaskExtensions
{
    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Reviewed")]
    public static void SafeWait(this Task task, CancellationToken cancellationToken = default)
    {
        if (task.IsCompletedSuccessfully)
            return;

        SpinUntilCompleted(task, cancellationToken);

        task.GetAwaiter().GetResult();
    }

    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Reviewed")]
    public static TResult SafeWait<TResult>(this Task<TResult> task, CancellationToken cancellationToken = default)
    {
        if (task.IsCompletedSuccessfully)
            return task.Result;

        SpinUntilCompleted(task, cancellationToken);

        return task.GetAwaiter().GetResult();
    }

    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Reviewed")]
    public static void SafeWait(this ValueTask valueTask, CancellationToken cancellationToken = default)
    {
        if (valueTask.IsCompletedSuccessfully)
            return;

        SpinUntilCompleted(valueTask, cancellationToken);

        valueTask.GetAwaiter().GetResult();
    }

    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Reviewed")]
    public static TResult SafeWait<TResult>(this ValueTask<TResult> valueTask, CancellationToken cancellationToken = default)
    {
        if (valueTask.IsCompletedSuccessfully)
            return valueTask.Result;

        SpinUntilCompleted(valueTask, cancellationToken);

        return valueTask.GetAwaiter().GetResult();
    }

    private static void SpinUntilCompleted(Task task, CancellationToken cancellationToken)
    {
        SpinWait spinner = default;

        while (!task.IsCompleted)
        {
            cancellationToken.ThrowIfCancellationRequested();
            spinner.SpinOnce();
        }
    }

    private static void SpinUntilCompleted(ValueTask task, CancellationToken cancellationToken)
    {
        SpinWait spinner = default;

        while (!task.IsCompleted)
        {
            cancellationToken.ThrowIfCancellationRequested();
            spinner.SpinOnce();
        }
    }

    private static void SpinUntilCompleted<T>(ValueTask<T> task, CancellationToken cancellationToken)
    {
        SpinWait spinner = default;

        while (!task.IsCompleted)
        {
            cancellationToken.ThrowIfCancellationRequested();
            spinner.SpinOnce();
        }
    }
}
