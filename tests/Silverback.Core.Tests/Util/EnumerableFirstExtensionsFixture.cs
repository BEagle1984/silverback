// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class EnumerableFirstExtensionsFixture
{
    [Fact]
    public async Task FirstOrDefaultAsync_ShouldReturnMatchingItem()
    {
        object?[] enumerable = { "one", 1, null };

        object? result = await enumerable.FirstOrDefaultAsync(item => ValueTask.FromResult(item is int));

        result.Should().Be(1);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ShouldReturnNull_WhenNoMatchingObjectIsFound()
    {
        object?[] enumerable = { "one", 1, null };

        object? result = await enumerable.FirstOrDefaultAsync(item => ValueTask.FromResult(item is int intItem && intItem == 2));

        result.Should().BeNull();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ShouldReturnZero_WhenNoMatchingIntIsFound()
    {
        int[] enumerable = { 1, 3, 5, 7, 9 };

        int result = await enumerable.FirstOrDefaultAsync(item => ValueTask.FromResult(item % 2 == 0));

        result.Should().Be(0);
    }
}
