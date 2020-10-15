// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
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
        public void ForEach_Action_Enumerated()
        {
            var enumerable = Enumerable.Range(1, 5);

            var total = 0;
            enumerable.ForEach(i => total += i);

            total.Should().Be(15);
        }

        [Fact]
        public async Task ForEachAsync_EnumerableWithAsyncFunc_Enumerated()
        {
            var enumerable = Enumerable.Range(1, 5);

            var total = 0;
            await enumerable.ForEachAsync(
                async i =>
                {
                    await Task.Delay(1);
                    total += i;
                });

            total.Should().Be(15);
        }

        [Fact]
        public void ForEachAsync_AsyncEnumerableWithAction_Enumerated()
        {
            var enumerable = AsyncEnumerable.Range(1, 5);

            var total = 0;
            enumerable.ForEachAsync(i => total += i);

            total.Should().Be(15);
        }

        [Fact]
        public async Task ForEachAsync_AsyncEnumerableWithAsyncFunc_Enumerated()
        {
            var enumerable = AsyncEnumerable.Range(1, 5);

            var total = 0;
            await enumerable.ForEachAsync(
                async i =>
                {
                    await Task.Delay(1);
                    total += i;
                });

            total.Should().Be(15);
        }


        [Fact]
        public async Task ParallelForEach_AsyncFunc_Enumerated()
        {
            var enumerable = Enumerable.Range(1, 5);

            var total = 0;
            await enumerable.ParallelForEachAsync(
                async i =>
                {
                    await Task.Delay(1);
                    Interlocked.Add(ref total, i);
                });

            total.Should().Be(15);
        }
    }
}
