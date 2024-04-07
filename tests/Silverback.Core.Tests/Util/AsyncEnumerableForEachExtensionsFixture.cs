// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class AsyncEnumerableForEachExtensionsFixture
{
    [Fact]
    public async Task ForEachAsync_ShouldInvokeActionForEachItemInAsyncEnumerable()
    {
        IAsyncEnumerable<int> enumerable = AsyncEnumerable.Range(1, 5);
        int total = 0;

        await enumerable.ForEachAsync(item => total += item);

        total.Should().Be(15);
    }

    [Fact]
    public async Task ForEachAsync_ShouldInvokeAsyncFunctionForEachItemInAsyncEnumerable_WhenFuncReturnsTask()
    {
        IAsyncEnumerable<int> enumerable = AsyncEnumerable.Range(1, 5);
        int total = 0;

        async Task Do(int item)
        {
            await Task.Delay(1);
            total += item;
        }

        await enumerable.ForEachAsync(Do);

        total.Should().Be(15);
    }

    [Fact]
    public async Task ForEachAsync_ShouldInvokeAsyncFunctionForEachItemInAsyncEnumerable_WhenFuncReturnsValueTask()
    {
        IAsyncEnumerable<int> enumerable = AsyncEnumerable.Range(1, 5);
        int total = 0;

        async ValueTask Do(int item)
        {
            await Task.Delay(1);
            total += item;
        }

        await enumerable.ForEachAsync(Do);

        total.Should().Be(15);
    }
}
