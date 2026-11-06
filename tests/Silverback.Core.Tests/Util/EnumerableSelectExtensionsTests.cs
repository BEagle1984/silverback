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

public class EnumerableSelectExtensionsTests
{
    [Fact]
    public async Task ParallelSelectAsync_ShouldInvokeAsyncFunctionInParallelAndReturnSelectedValues()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        CountdownEvent countdownEvent = new(3);

        IEnumerable<int> result = await enumerable.ParallelSelectAsync(async item =>
        {
            await Task.Delay(1);

            countdownEvent.Signal();
            countdownEvent.WaitOrThrow();

            return item * 2;
        });

        result.ShouldBe([2, 4, 6], true);
    }

    [Fact]
    public async Task ParallelSelectAsync_ShouldInvokeSynchronousPartInParallel()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        CountdownEvent countdownEvent = new(3);

        IEnumerable<int> result = await enumerable.ParallelSelectAsync(item =>
        {
            countdownEvent.Signal();
            countdownEvent.WaitOrThrow();

            return ValueTask.FromResult(item * 2);
        });

        result.ShouldBe([2, 4, 6], true);
    }

    [Fact]
    public async Task ParallelSelectAsync_ShouldReturnResultsInSourceOrder()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 10);

        IEnumerable<int> result = await enumerable.ParallelSelectAsync(async item =>
        {
            await Task.Delay((11 - item) * 10);

            return item * 2;
        });

        result.ShouldBe([2, 4, 6, 8, 10, 12, 14, 16, 18, 20]);
    }

    [Fact]
    public async Task ParallelSelectAsync_ShouldReturnWithoutBlockingUntilSynchronousPartCompletes()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        ManualResetEventSlim releaseEvent = new();

        ValueTask<IReadOnlyCollection<int>> resultTask = enumerable.ParallelSelectAsync(item =>
        {
            releaseEvent.Wait(TimeSpan.FromSeconds(10));

            return ValueTask.FromResult(item * 2);
        });

        resultTask.IsCompleted.ShouldBeFalse();

        releaseEvent.Set();

        IEnumerable<int> result = await resultTask;
        result.ShouldBe([2, 4, 6]);
    }

    [Fact]
    public async Task ParallelSelectAsync_ShouldAwaitAllAndAggregateExceptions_WhenSelectorThrowsSynchronously()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        TaskCompletionSource<int> asyncItemCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        async ValueTask<int> ThrowAsync()
        {
            await asyncItemCompletionSource.Task;

            throw new InvalidOperationException("async");
        }

        ValueTask<IReadOnlyCollection<int>> resultTask = enumerable.ParallelSelectAsync(item =>
            item == 2
                ? ThrowAsync()
                : throw new ArgumentException("sync"));

        await Task.Delay(100);
        resultTask.IsCompleted.ShouldBeFalse();

        asyncItemCompletionSource.SetResult(0);

        AggregateException exception = await Should.ThrowAsync<AggregateException>(async () => await resultTask);
        exception.InnerExceptions.Select(innerException => innerException.Message).ShouldBe(["sync", "async", "sync"]);
    }

    [Fact]
    public async Task SelectAsync_ShouldInvokeAsyncFunctionAndReturnSelectedValues()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);

        IEnumerable<int> result = await enumerable.SelectAsync(item => ValueTask.FromResult(item * 2));

        result.ShouldBe([2, 4, 6]);
    }
}
