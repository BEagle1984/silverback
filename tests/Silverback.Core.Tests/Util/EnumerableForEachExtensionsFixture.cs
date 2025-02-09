// Copyright (c) 2024 Sergio Aquilini
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

public class EnumerableForEachExtensionsFixture
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

        enumerable.ForEach(
            (item, index) =>
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
    public void ParallelForEach_ShouldInvokeActionInParallelForEachItemInEnumerable()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        CountdownEvent countdownEvent = new(3);
        int total = 0;

        enumerable.ParallelForEach(
            item =>
            {
                countdownEvent.Signal();
                countdownEvent.WaitOrThrow();

                Interlocked.Add(ref total, item);
            });

        total.ShouldBe(6);
    }

    [Fact]
    public void ParallelForEach_ShouldInvokeActionWithLimitedParallelism()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        CountdownEvent countdownEvent = new(3);

        Action act = () => enumerable.ParallelForEach(
            _ =>
            {
                countdownEvent.Signal();
                countdownEvent.WaitOrThrow(TimeSpan.FromMilliseconds(100));
            },
            2);

        AggregateException aggregateException = act.ShouldThrow<AggregateException>();
        aggregateException.InnerExceptions.ShouldAllBe(exception => exception is TimeoutException);
        countdownEvent.CurrentCount.ShouldBe(1);
    }

    [Fact]
    public async Task ParallelForEachAsync_ShouldInvokeAsyncFunctionInParallelForEachItemInEnumerable()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        CountdownEvent countdownEvent = new(3);
        int total = 0;

        await enumerable.ParallelForEachAsync(
            async item =>
            {
                await Task.Delay(1);

                countdownEvent.Signal();
                countdownEvent.WaitOrThrow();

                Interlocked.Add(ref total, item);
            });

        total.ShouldBe(6);
    }

    [Fact]
    public async Task ParallelForEachAsync_ShouldInvokeAsyncFunctionWithLimitedParallelism()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        CountdownEvent countdownEvent = new(3);

        Func<Task> act = () => enumerable.ParallelForEachAsync(
            async _ =>
            {
                await Task.Delay(1);

                countdownEvent.Signal();
                countdownEvent.WaitOrThrow(TimeSpan.FromMilliseconds(100));
            },
            2).AsTask();

        AggregateException aggregateException = await act.ShouldThrowAsync<AggregateException>();
        aggregateException.InnerExceptions.ShouldAllBe(exception => exception is TimeoutException || exception is OperationCanceledException);
        countdownEvent.CurrentCount.ShouldBe(1);
    }

    [Fact]
    public async Task ParallelForEach_ShouldBlockUntilAsyncFunctionCompletes()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);

        int total = 0;
        await enumerable.ParallelForEachAsync(
            async i =>
            {
                await Task.Delay(500);
                Interlocked.Add(ref total, i);
            },
            2);

        total.ShouldBe(15);
    }
}
