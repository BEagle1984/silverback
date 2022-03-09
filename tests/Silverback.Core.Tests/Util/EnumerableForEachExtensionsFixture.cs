// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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

        total.Should().Be(15);
    }

    [Fact]
    public void ForEach_ShouldInvokeActionAndProvideIndexForEachItemInEnumerable()
    {
        IEnumerable<int> enumerable = Enumerable.Range(0, 5);
        int total = 0;

        enumerable.ForEach(
            (item, index) =>
            {
                index.Should().Be(item);
                total += item;
            });

        total.Should().Be(10);
    }

    [Fact]
    public async Task ForEachAsync_ShouldInvokeAsyncFunctionForEachItemInEnumerable()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);
        int total = 0;

        await enumerable.ForEachAsync(
            async i =>
            {
                await Task.Delay(1);
                total += i;
            });

        total.Should().Be(15);
    }

    [Fact]
    public void ParallelForEach_ShouldInvokeActionInParallelForEachItemInEnumerable()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        CountdownEvent countdownEvent = new(3);
        ConcurrentBag<int> threads = new();
        int total = 0;

        enumerable.ParallelForEach(
            item =>
            {
                countdownEvent.Signal();
                countdownEvent.Wait(TimeSpan.FromSeconds(1));
                threads.Add(Thread.CurrentThread.ManagedThreadId);

                Interlocked.Add(ref total, item);
            });

        total.Should().Be(6);
        threads.Distinct().Should().HaveCount(3);
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

                if (!countdownEvent.Wait(TimeSpan.FromMilliseconds(100)))
                    throw new TimeoutException();
            },
            2);

        act.Should().Throw<TimeoutException>();
        countdownEvent.CurrentCount.Should().Be(1);
    }

    [Fact]
    public async Task ParallelForEachAsync_ShouldInvokeAsyncFunctionInParallelForEachItemInEnumerable()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        CountdownEvent countdownEvent = new(3);
        ConcurrentBag<int> threads = new();
        int total = 0;

        await enumerable.ParallelForEachAsync(
            async item =>
            {
                await Task.Delay(1);

                countdownEvent.Signal();
                countdownEvent.Wait(TimeSpan.FromSeconds(1));
                threads.Add(Thread.CurrentThread.ManagedThreadId);

                Interlocked.Add(ref total, item);
            });

        total.Should().Be(6);
        threads.Distinct().Should().HaveCount(3);
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

                if (!countdownEvent.Wait(TimeSpan.FromMilliseconds(100)))
                    throw new TimeoutException();
            },
            2);

        await act.Should().ThrowAsync<TimeoutException>();
        countdownEvent.CurrentCount.Should().Be(1);
    }
}
