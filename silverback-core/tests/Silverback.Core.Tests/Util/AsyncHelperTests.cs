// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Util;

namespace Silverback.Tests.Util
{
    [TestFixture]
    public class AsyncHelperTests
    {
        [Test]
        public void RunSynchronouslyTest()
        {
            var done = false;

            AsyncHelper.RunSynchronously(AsyncMethod);

            Assert.IsTrue(done);

            async Task AsyncMethod()
            {
                await Task.Delay(50);
                done = true;
            }
        }

        [Test]
        public void RunSynchronouslyWithResultTest()
        {
            var result = AsyncHelper.RunSynchronously(AsyncMethod);

            Assert.AreEqual(3, result);

            async Task<int> AsyncMethod()
            {
                await Task.Delay(50);
                return 3;
            }
        }

        [Test]
        public void RunSynchronouslyWithArgumentTest()
        {
            int result = 1;
            AsyncHelper.RunSynchronously(() => AsyncMethod(2));

            Assert.AreEqual(3, result);

            async Task AsyncMethod(int arg)
            {
                await Task.Delay(50);
                result += arg;
            }
        }

        [Test]
        public void RunSynchronously_NoReturn_ThrowsException()
        {
            Assert.Throws<AggregateException>(() => AsyncHelper.RunSynchronously(AsyncMethod));

            async Task AsyncMethod()
            {
                await Task.Delay(50);
                throw new Exception("test");
            }
        }

        [Test]
        public void RunSynchronously_WithReturn_ThrowsException()
        {
            Assert.Throws<AggregateException>(() => AsyncHelper.RunSynchronously(AsyncMethod));

            async Task<int> AsyncMethod()
            {
                await Task.Delay(50);
                throw new Exception("test");
            }
        }
    }
}
