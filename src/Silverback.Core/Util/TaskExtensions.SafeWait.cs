// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static partial class TaskExtensions
{
    private static readonly TaskFactory TaskFactory =
        new(CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);

    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Reviewed")]
    public static void SafeWait(this Task task, CancellationToken cancellationToken = default)
    {
        if (task.IsCompletedSuccessfully)
            return;

        TaskFactory
            .StartNew(() => task, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default)
            .Unwrap()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Reviewed")]
    public static TResult SafeWait<TResult>(this Task<TResult> task, CancellationToken cancellationToken = default)
    {
        if (task.IsCompletedSuccessfully)
            return task.Result;

        return TaskFactory
            .StartNew(() => task, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default)
            .Unwrap()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Reviewed")]
    public static void SafeWait(this ValueTask valueTask, CancellationToken cancellationToken = default)
    {
        if (valueTask.IsCompletedSuccessfully)
            return;

        TaskFactory
            .StartNew(valueTask.AsTask, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default)
            .Unwrap()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Reviewed")]
    public static TResult SafeWait<TResult>(this ValueTask<TResult> valueTask, CancellationToken cancellationToken = default)
    {
        if (valueTask.IsCompletedSuccessfully)
            return valueTask.GetAwaiter().GetResult();

        return TaskFactory
            .StartNew(valueTask.AsTask, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default)
            .Unwrap()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }
}
