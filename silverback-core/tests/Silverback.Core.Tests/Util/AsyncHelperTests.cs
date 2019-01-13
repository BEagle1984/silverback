// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Core.Tests.Util
{
    [Collection("Core.Util")]
    public class AsyncHelperTests
    {
        [Fact]
        public void RunSynchronously_VoidMethod_Executed()
        {
            var done = false;

            AsyncHelper.RunSynchronously(AsyncMethod);

            done.Should().BeTrue();

            async Task AsyncMethod()
            {
                await Task.Delay(50);
                done = true;
            }
        }

        [Fact]
        public void RunSynchronously_MethodWithResult_Executed()
        {
            var result = AsyncHelper.RunSynchronously(AsyncMethod);

            result.Should().Be(3);

            async Task<int> AsyncMethod()
            {
                await Task.Delay(50);
                return 3;
            }
        }

        [Fact]
        public void RunSynchronously_MethodWithArgument_Executed()
        {
            int result = 1;
            AsyncHelper.RunSynchronously(() => AsyncMethod(2));

            result.Should().Be(3);

            async Task AsyncMethod(int arg)
            {
                await Task.Delay(50);
                result += arg;
            }
        }

        [Fact]
        public void RunSynchronously_NoReturn_ThrowsException()
        {
            Action act = () => AsyncHelper.RunSynchronously(AsyncMethod);

            act.Should().Throw<AggregateException>();

            async Task AsyncMethod()
            {
                await Task.Delay(50);
                throw new Exception("test");
            }
        }

        [Fact]
        public void RunSynchronously_WithReturn_ThrowsException()
        {
            Action act = () => AsyncHelper.RunSynchronously(AsyncMethod);

            act.Should().Throw<AggregateException>();

            async Task<int> AsyncMethod()
            {
                await Task.Delay(50);
                throw new Exception("test");
            }
        }
    }
}
