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

public class EnumerableSelectExtensionsFixture
{
    [Fact]
    public void ParallelSelect_ShouldInvokeActionInParallelAndReturnSelectedValues()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);
        CountdownEvent countdownEvent = new(5);
        ConcurrentBag<int> threads = new();

        IEnumerable<int> result = enumerable.ParallelSelect(
            item =>
            {
                countdownEvent.Signal();
                countdownEvent.Wait(TimeSpan.FromSeconds(1));
                threads.Add(Thread.CurrentThread.ManagedThreadId);

                return item * 2;
            });

        result.Should().BeEquivalentTo(new[] { 2, 4, 6, 8, 10 });
        threads.Distinct().Should().HaveCount(5);
    }

    [Fact]
    public void ParallelSelect_ShouldInvokeActionWithLimitedParallelismAndReturnSelectedValues()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 6);
        CountdownEvent countdownEvent = new(5);

        Action act = () => enumerable.ParallelSelect(
            item =>
            {
                countdownEvent.Signal();

                if (!countdownEvent.Wait(TimeSpan.FromMilliseconds(100)))
                    throw new TimeoutException();

                return item * 2;
            },
            2);

        act.Should().Throw<TimeoutException>();
        countdownEvent.CurrentCount.Should().Be(countdownEvent.InitialCount - 2);
    }

    [Fact]
    public async Task ParallelSelectAsync_ShouldInvokeAsyncFunctionInParallelAndReturnSelectedValues()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);
        CountdownEvent countdownEvent = new(5);
        ConcurrentBag<int> threads = new();

        IEnumerable<int> result = await enumerable.ParallelSelectAsync(
            async item =>
            {
                await Task.Delay(1);

                countdownEvent.Signal();
                countdownEvent.Wait(TimeSpan.FromSeconds(1));
                threads.Add(Thread.CurrentThread.ManagedThreadId);

                return item * 2;
            });

        result.Should().BeEquivalentTo(new[] { 2, 4, 6, 8, 10 });
        threads.Distinct().Should().HaveCount(5);
    }

    [Fact]
    public async Task ParallelSelectAsync_ShouldInvokeAsyncFunctionWithLimitedParallelismAndReturnSelectedValues()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);
        CountdownEvent countdownEvent = new(5);

        Func<Task> act = () => enumerable.ParallelSelectAsync(
            async item =>
            {
                await Task.Delay(1);

                countdownEvent.Signal();

                if (!countdownEvent.Wait(TimeSpan.FromMilliseconds(100)))
                    throw new TimeoutException();

                return item * 2;
            },
            2);

        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Select_ShouldInvokeActionAndReturnSelectedValuesInParallelOrSequentially(bool parallel)
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);

        IEnumerable<int> result = enumerable.Select(item => item * 2, parallel);

        result.Should().BeEquivalentTo(new[] { 2, 4, 6, 8, 10 });
    }

    [Fact]
    public async Task SelectAsync_ShouldInvokeAsyncFunctionAndReturnSelectedValues()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);

        IEnumerable<int> result = await enumerable.SelectAsync(item => Task.FromResult(item * 2));

        result.Should().BeEquivalentTo(new[] { 2, 4, 6, 8, 10 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SelectAsync_ShouldInvokeAsyncFunctionAndReturnSelectedValuesInParallelOrSequentially(bool parallel)
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);

        IEnumerable<int> result = await enumerable.SelectAsync(i => Task.FromResult(i * 2), parallel);

        result.Should().BeEquivalentTo(new[] { 2, 4, 6, 8, 10 });
    }

    [Fact]
    public async Task SelectManyAsync_ShouldInvokeAsyncFunctionAndReturnFlattenedSelectedValues()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);

        IEnumerable<int> result =
            await enumerable.SelectManyAsync(i => Task.FromResult(new[] { i, i }.AsEnumerable()));

        result.Should().BeEquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 });
    }

    [Fact]
    public async Task ParallelSelectManyAsync_Function_Selected()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);

        IEnumerable<int> result =
            await enumerable.ParallelSelectManyAsync(i => Task.FromResult(new[] { i, i }.AsEnumerable()));

        result.Should().BeEquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 });
    }

    [Fact]
    public async Task ParallelSelectManyAsync_ShouldInvokeAsyncFunctionInParallelAndReturnFlattenedSelectedValues()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);
        CountdownEvent countdownEvent = new(5);
        ConcurrentBag<int> threads = new();

        IEnumerable<int> result = await enumerable.ParallelSelectManyAsync<int, int>(
            async item =>
            {
                await Task.Delay(1);

                countdownEvent.Signal();
                countdownEvent.Wait(TimeSpan.FromSeconds(1));
                threads.Add(Thread.CurrentThread.ManagedThreadId);

                return new[] { item * 2, item * 3 };
            });

        result.Should().BeEquivalentTo(new[] { 2, 3, 4, 6, 6, 9, 8, 12, 10, 15 });
        threads.Distinct().Should().HaveCount(5);
    }

    [Fact]
    public async Task ParallelSelectManyAsync_ShouldInvokeAsyncFunctionWithLimitedParallelism()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);
        CountdownEvent countdownEvent = new(5);

        Func<Task> act = () => enumerable.ParallelSelectManyAsync<int, int>(
            async item =>
            {
                await Task.Delay(1);

                countdownEvent.Signal();

                if (!countdownEvent.Wait(TimeSpan.FromMilliseconds(100)))
                    throw new TimeoutException();

                return new[] { item * 2, item * 3 };
            },
            2);

        await act.Should().ThrowAsync<TimeoutException>();
    }
}
