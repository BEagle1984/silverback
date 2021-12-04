// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class EnumerableSelectExtensionsTests
{
    [Fact]
    public void ParallelSelect_Function_Selected()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);

        IEnumerable<int> result = enumerable.ParallelSelect(i => i * 2);

        result.Should().BeEquivalentTo(new[] { 2, 4, 6, 8, 10 });
    }

    [Fact]
    public async Task ParallelSelectAsync_Function_Selected()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);

        IEnumerable<int> result = await enumerable.ParallelSelectAsync(i => Task.FromResult(i * 2));

        result.Should().BeEquivalentTo(new[] { 2, 4, 6, 8, 10 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Select_ParallelAndNotParallel_Selected(bool parallel)
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);

        IEnumerable<int> result = enumerable.Select(i => i * 2, parallel);

        result.Should().BeEquivalentTo(new[] { 2, 4, 6, 8, 10 });
    }

    [Fact]
    public async Task SelectAsync_Function_Selected()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);

        IEnumerable<int> result = await enumerable.SelectAsync(i => Task.FromResult(i * 2));

        result.Should().BeEquivalentTo(new[] { 2, 4, 6, 8, 10 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SelectAsync_ParallelAndNotParallel_Selected(bool parallel)
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 5);

        IEnumerable<int> result = await enumerable.SelectAsync(i => Task.FromResult(i * 2), parallel);

        result.Should().BeEquivalentTo(new[] { 2, 4, 6, 8, 10 });
    }

    [Fact]
    public async Task SelectManyAsync_Function_Selected()
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
}
