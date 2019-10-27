// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util
{
    public class EnumerableForEachExtensions
    {
        [Fact]
        public void ForEachTest()
        {
            var array = (IEnumerable<int>) new[] {1, 2, 3, 4, 5};

            var total = 0;
            array.ForEach(i => total += i);

            total.Should().Be(15);
        }

        [Fact]
        public async Task ForEachAsyncTest()
        {
            var array = (IEnumerable<int>)new[] { 1, 2, 3, 4, 5 };

            var total = 0;
            await array.ForEachAsync(async i =>
            {
                await Task.Delay(1);
                total += i;
            });

            total.Should().Be(15);
        }
    }
}