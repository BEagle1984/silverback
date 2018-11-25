// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Util;

namespace Silverback.Tests.Util
{
    [TestFixture]
    public class EnumerableExtensionsTests
    {
        [Test]
        public void ForEachTest()
        {
            var array = (IEnumerable<int>) new[] {1, 2, 3, 4, 5};

            var total = 0;
            array.ForEach(i => total += i);

            Assert.That(total, Is.EqualTo(15));
        }

        [Test]
        public async Task ForEachAsyncTest()
        {
            var array = (IEnumerable<int>)new[] { 1, 2, 3, 4, 5 };

            var total = 0;
            await array.ForEachAsync(async i =>
            {
                await Task.Delay(1);
                total += i;
            });

            Assert.That(total, Is.EqualTo(15));
        }
    }
}