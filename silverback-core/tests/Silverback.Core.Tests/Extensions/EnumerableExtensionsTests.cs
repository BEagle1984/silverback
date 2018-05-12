using System.Collections.Generic;
using NUnit.Framework;

namespace Silverback.Tests.Extensions
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

        }
        
    }
}