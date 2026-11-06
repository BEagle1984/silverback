// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class EnumerableForEachExtensionsTests
{
    [Fact]
    public void ForEach_ShouldInvokeActionForEachItemInEnumerable()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);
        int total = 0;

        enumerable.ForEach(i => total += i);

        total.ShouldBe(15);
    }

    [Fact]
    public void ForEach_ShouldInvokeActionAndProvideIndexForEachItemInEnumerable()
    {
        IEnumerable<int> enumerable = Enumerable.Range(0, 5);
        int total = 0;

        enumerable.ForEach((item, index) =>
        {
            index.ShouldBe(item);
            total += item;
        });

        total.ShouldBe(10);
    }

    [Fact]
    public async Task ForEachAsync_ShouldInvokeAsyncFunctionForEachItemInEnumerable_WhenFuncReturnsTask()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);
        int total = 0;

        async Task Do(int i)
        {
            await Task.Delay(1);
            total += i;
        }

        await enumerable.ForEachAsync(Do);

        total.ShouldBe(15);
    }

    [Fact]
    public async Task ForEachAsync_ShouldInvokeAsyncFunctionForEachItemInEnumerable_WhenFuncReturnsValueTask()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);
        int total = 0;

        async ValueTask Do(int i)
        {
            await Task.Delay(1);
            total += i;
        }

        await enumerable.ForEachAsync(Do);

        total.ShouldBe(15);
    }

    [Fact]
    public async Task ParallelForEachAsync_ShouldInvokeAsyncFunctionInParallelForEachItemInEnumerable()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        CountdownEvent countdownEvent = new(3);
        int total = 0;

        await enumerable.ParallelForEachAsync(async item =>
        {
            await Task.Delay(1);

            countdownEvent.Signal();
            countdownEvent.WaitOrThrow();

            Interlocked.Add(ref total, item);
        });

        total.ShouldBe(6);
    }

    [Fact]
    public async Task ParallelForEach_ShouldBlockUntilAsyncFunctionCompletes()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);

        int total = 0;
        await enumerable.ParallelForEachAsync(async i =>
        {
            await Task.Delay(500);
            Interlocked.Add(ref total, i);
        });

        total.ShouldBe(15);
    }

    [Fact]
    public async Task ParallelForEachAsync_ShouldInvokeSynchronousPartInParallel()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        CountdownEvent countdownEvent = new(3);

        await enumerable.ParallelForEachAsync(_ =>
        {
            countdownEvent.Signal();
            countdownEvent.WaitOrThrow();

            return ValueTask.CompletedTask;
        });

        countdownEvent.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public async Task ParallelForEachAsync_ShouldAwaitAllAndAggregateExceptions_WhenActionThrowsSynchronously()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        TaskCompletionSource asyncItemCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        async ValueTask ThrowAsync()
        {
            await asyncItemCompletionSource.Task;

            throw new InvalidOperationException("async");
        }

        ValueTask task = enumerable.ParallelForEachAsync(item =>
            item == 2
                ? ThrowAsync()
                : throw new ArgumentException("sync"));

        await Task.Delay(100);
        task.IsCompleted.ShouldBeFalse();

        asyncItemCompletionSource.SetResult();

        AggregateException exception = await Should.ThrowAsync<AggregateException>(async () => await task);
        exception.InnerExceptions.Select(innerException => innerException.Message).ShouldBe(["sync", "async", "sync"]);
    }
}
