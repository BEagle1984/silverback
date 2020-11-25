// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util
{
    public class AsyncHelperTests
    {
        [Fact]
        public void RunSynchronously_ReturningTask_Executed()
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
        public void RunSynchronously_ReturningValueTask_Executed()
        {
            var done = false;

            AsyncHelper.RunSynchronously(AsyncMethod);

            done.Should().BeTrue();

            async ValueTask AsyncMethod()
            {
                await Task.Delay(50);
                done = true;
            }
        }

        [Fact]
        public void RunSynchronously_ReturningTaskWithResult_Executed()
        {
            var result = AsyncHelper.RunSynchronously(AsyncMethod);

            result.Should().Be(3);

            static async Task<int> AsyncMethod()
            {
                await Task.Delay(50);
                return 3;
            }
        }

        [Fact]
        public void RunSynchronously_ReturningValueTaskWithResult_Executed()
        {
            var result = AsyncHelper.RunSynchronously(AsyncMethod);

            result.Should().Be(3);

            static async ValueTask<int> AsyncMethod()
            {
                await Task.Delay(50);
                return 3;
            }
        }

        [Fact]
        public void RunSynchronously_WithArgumentReturningTask_Executed()
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
        public void RunSynchronously_WithArgumentReturningValueTask_Executed()
        {
            int result = 1;
            AsyncHelper.RunSynchronously(() => AsyncMethod(2));

            result.Should().Be(3);

            async ValueTask AsyncMethod(int arg)
            {
                await Task.Delay(50);
                result += arg;
            }
        }

        [Fact]
        public void RunSynchronously_ReturningTaskButThrowingException_ExceptionRethrown()
        {
            Action act = () => AsyncHelper.RunSynchronously(AsyncMethod);

            act.Should().Throw<NotSupportedException>();

            static async Task AsyncMethod()
            {
                await Task.Delay(50);
                throw new NotSupportedException("test");
            }
        }

        [Fact]
        public void RunSynchronously_ReturningValueTaskButThrowingException_ExceptionRethrown()
        {
            Action act = () => AsyncHelper.RunSynchronously(AsyncMethod);

            act.Should().Throw<NotSupportedException>();

            static async ValueTask AsyncMethod()
            {
                await Task.Delay(50);
                throw new NotSupportedException("test");
            }
        }

        [Fact]
        public void RunSynchronously_ReturningTaskWithResultButThrowingException_ExceptionRethrown()
        {
            Action act = () => AsyncHelper.RunSynchronously(AsyncMethod);

            act.Should().Throw<NotSupportedException>();

            static async Task<int> AsyncMethod()
            {
                await Task.Delay(50);
                throw new NotSupportedException("test");
            }
        }

        [Fact]
        public void RunSynchronously_ReturningValueTaskWithResultButThrowingException_ExceptionRethrown()
        {
            Action act = () => AsyncHelper.RunSynchronously(AsyncMethod);

            act.Should().Throw<NotSupportedException>();

            static async ValueTask<int> AsyncMethod()
            {
                await Task.Delay(50);
                throw new NotSupportedException("test");
            }
        }
    }
}
