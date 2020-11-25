// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util
{
    public class EnumerableWhereExtensionsTests
    {
        [Fact]
        public async Task WhereAsync_Function_Filtered()
        {
            var enumerable = Enumerable.Range(1, 5);

            var result = await enumerable.WhereAsync(i => Task.FromResult(i % 2 == 0));

            result.Should().BeEquivalentTo(2, 4);
        }

        [Fact]
        public void WhereNotNull_CollectionWithSomeNulls_Filtered()
        {
            var enumerable = new[] { "one", null, "two" };

            var result = enumerable.WhereNotNull();

            result.Should().BeEquivalentTo("one", "two");
        }
    }
}
