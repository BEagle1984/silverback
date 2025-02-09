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

public class EnumerableSelectExtensionsFixture
{
    [Fact]
    public void ParallelSelect_ShouldInvokeActionInParallelAndReturnSelectedValues()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        CountdownEvent countdownEvent = new(3);

        IEnumerable<int> result = enumerable.ParallelSelect(
            item =>
            {
                countdownEvent.Signal();
                countdownEvent.WaitOrThrow();

                return item * 2;
            });

        result.ShouldBe([2, 4, 6], ignoreOrder: true);
    }

    [Fact]
    public void ParallelSelect_ShouldInvokeActionWithLimitedParallelismAndReturnSelectedValues()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        CountdownEvent countdownEvent = new(3);

        Action act = () => enumerable.ParallelSelect(
            item =>
            {
                countdownEvent.Signal();
                countdownEvent.WaitOrThrow(TimeSpan.FromMilliseconds(100));

                return item * 2;
            },
            2);

        AggregateException aggregateException = act.ShouldThrow<AggregateException>();
        aggregateException.InnerExceptions.Count.ShouldBe(2);
        aggregateException.InnerExceptions.ShouldAllBe(exception => exception is TimeoutException);
        countdownEvent.CurrentCount.ShouldBe(1);
    }

    [Fact]
    public async Task ParallelSelectAsync_ShouldInvokeAsyncFunctionInParallelAndReturnSelectedValues()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        CountdownEvent countdownEvent = new(3);

        IEnumerable<int> result = await enumerable.ParallelSelectAsync(
            async item =>
            {
                await Task.Delay(1);

                countdownEvent.Signal();
                countdownEvent.WaitOrThrow();

                return item * 2;
            });

        result.ShouldBe([2, 4, 6], ignoreOrder: true);
    }

    [Fact]
    public async Task ParallelSelectAsync_ShouldInvokeAsyncFunctionWithLimitedParallelismAndReturnSelectedValues()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        CountdownEvent countdownEvent = new(3);

        Func<Task> act = () => enumerable.ParallelSelectAsync(
            async item =>
            {
                await Task.Delay(1);

                countdownEvent.Signal();
                countdownEvent.WaitOrThrow(TimeSpan.FromMilliseconds(100));

                return item * 2;
            },
            2).AsTask();

        AggregateException aggregateException = await act.ShouldThrowAsync<AggregateException>();
        aggregateException.InnerExceptions.Count.ShouldBe(3);
        aggregateException.InnerExceptions.ShouldAllBe(exception => exception is TimeoutException || exception is OperationCanceledException);
        countdownEvent.CurrentCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Select_ShouldInvokeActionAndReturnSelectedValuesInParallelOrSequentially(bool parallel)
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);

        IEnumerable<int> result = enumerable.Select(item => item * 2, parallel);

        result.ShouldBe([2, 4, 6], ignoreOrder: true);
    }

    [Fact]
    public async Task SelectAsync_ShouldInvokeAsyncFunctionAndReturnSelectedValues()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);

        IEnumerable<int> result = await enumerable.SelectAsync(item => ValueTask.FromResult(item * 2));

        result.ShouldBe([2, 4, 6]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SelectAsync_ShouldInvokeAsyncFunctionAndReturnSelectedValuesInParallelOrSequentially(bool parallel)
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);

        IEnumerable<int> result = await enumerable.SelectAsync(i => ValueTask.FromResult(i * 2), parallel);

        result.ShouldBe([2, 4, 6], ignoreOrder: true);
    }

    [Fact]
    public async Task SelectManyAsync_ShouldInvokeAsyncFunctionAndReturnFlattenedSelectedValues()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);

        IEnumerable<int> result =
            await enumerable.SelectManyAsync(i => ValueTask.FromResult(new[] { i, i }.AsEnumerable()));

        result.ShouldBe([1, 1, 2, 2, 3, 3]);
    }

    [Fact]
    public async Task ParallelSelectManyAsync_Function_Selected()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);

        IEnumerable<int> result = await enumerable.ParallelSelectManyAsync(i => ValueTask.FromResult(new[] { i, i }.AsEnumerable()));

        result.ShouldBe([1, 1, 2, 2, 3, 3], ignoreOrder: true);
    }

    [Fact]
    public async Task ParallelSelectManyAsync_ShouldInvokeAsyncFunctionInParallelAndReturnFlattenedSelectedValues()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        CountdownEvent countdownEvent = new(3);

        IEnumerable<int> result = await enumerable.ParallelSelectManyAsync<int, int>(
            async item =>
            {
                await Task.Delay(1);

                countdownEvent.Signal();
                countdownEvent.WaitOrThrow();

                return [item * 2, item * 3];
            });

        result.ShouldBe([2, 3, 4, 6, 6, 9], ignoreOrder: true);
    }

    [Fact]
    public async Task ParallelSelectManyAsync_ShouldInvokeAsyncFunctionWithLimitedParallelism()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        CountdownEvent countdownEvent = new(3);

        Func<Task> act = () => enumerable.ParallelSelectManyAsync<int, int>(
            async item =>
            {
                await Task.Delay(1);

                countdownEvent.Signal();
                countdownEvent.WaitOrThrow(TimeSpan.FromMilliseconds(100));

                return [item * 2, item * 3];
            },
            2).AsTask();

        AggregateException aggregateException = await act.ShouldThrowAsync<AggregateException>();
        aggregateException.InnerExceptions.Count.ShouldBe(3);
        aggregateException.InnerExceptions.ShouldAllBe(exception => exception is TimeoutException || exception is OperationCanceledException);
        countdownEvent.CurrentCount.ShouldBe(1);
    }
}
