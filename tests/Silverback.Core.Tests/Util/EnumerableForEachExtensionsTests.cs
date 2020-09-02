// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util
{
    public class EnumerableForEachExtensionsTests
    {
        [Fact]
        public void ForEach_Enumerable_Enumerated()
        {
            var array = (IEnumerable<int>)new[] { 1, 2, 3, 4, 5 };

            var total = 0;
            array.ForEach(i => total += i);

            total.Should().Be(15);
        }

        [Fact]
        public async Task ForEachAsync_Enumerable_Enumerated()
        {
            var array = (IEnumerable<int>)new[] { 1, 2, 3, 4, 5 };

            var total = 0;
            await array.ForEachAsync(
                async i =>
                {
                    await Task.Delay(1);
                    total += i;
                });

            total.Should().Be(15);
        }

        [Fact]
        public async Task ParallelForEachAsync_Enumerable_Enumerated()
        {
            var array = (IEnumerable<int>)new[] { 1, 2, 3, 4, 5 };

            var total = 0;
            await array.ParallelForEachAsync(
                async i =>
                {
                    await Task.Delay(1);
                    Interlocked.Add(ref total, i);
                });

            total.Should().Be(15);
        }
    }
}
