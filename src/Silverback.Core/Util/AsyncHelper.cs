// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Util;

// see https://stackoverflow.com/questions/9343594/how-to-call-asynchronous-method-from-synchronous-method-in-c
internal static class AsyncHelper
{
    private static readonly TaskFactory TaskFactory =
        new(CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);

    public static void RunSynchronously(Func<Task> asyncFunc) =>
        TaskFactory
            .StartNew(asyncFunc, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default)
            .Unwrap()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

    public static TResult RunSynchronously<TResult>(Func<Task<TResult>> asyncFunc) =>
        TaskFactory
            .StartNew(asyncFunc, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default)
            .Unwrap()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

    public static void RunSynchronously(Func<ValueTask> asyncFunc)
    {
        ValueTask valueTask = asyncFunc.Invoke();

        if (valueTask.IsCompleted)
            return;

        TaskFactory
            .StartNew(() => valueTask.AsTask(), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default)
            .Unwrap()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    public static TResult RunSynchronously<TResult>(Func<ValueTask<TResult>> asyncFunc)
    {
        ValueTask<TResult> valueTask = asyncFunc.Invoke();

        if (valueTask.IsCompleted)
            return valueTask.GetAwaiter().GetResult();

        return TaskFactory
            .StartNew(() => valueTask.AsTask(), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default)
            .Unwrap()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }
}
