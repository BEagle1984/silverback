// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class AsyncHelperFixture
{
    [Fact]
    public void RunSynchronously_ShouldExecuteAsyncMethodReturningTask()
    {
        bool done = false;

        AsyncHelper.RunSynchronously(AsyncMethod);

        done.Should().BeTrue();

        async Task AsyncMethod()
        {
            await Task.Delay(50);
            done = true;
        }
    }

    [Fact]
    public void RunSynchronously_ShouldExecuteAsyncMethodReturningTaskWithResult()
    {
        int result = AsyncHelper.RunSynchronously(AsyncMethod);

        result.Should().Be(3);

        static async Task<int> AsyncMethod()
        {
            await Task.Delay(50);
            return 3;
        }
    }

    [Fact]
    public void RunSynchronously_ShouldRethrow_WhenAsyncMethodReturningTaskThrows()
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
    public void RunSynchronously_ShouldRethrow_WhenAsyncMethodReturningTaskWithResultThrows()
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
    public void RunSynchronously_ShouldExecuteAsyncMethodReturningValueTask()
    {
        bool done = false;

        AsyncHelper.RunSynchronously(AsyncMethod);

        done.Should().BeTrue();

        async ValueTask AsyncMethod()
        {
            await Task.Delay(50);
            done = true;
        }
    }

    [Fact]
    public void RunSynchronously_ShouldExecuteSyncMethodReturningValueTask()
    {
        bool done = false;

        AsyncHelper.RunSynchronously(SyncMethod);

        done.Should().BeTrue();

        ValueTask SyncMethod()
        {
            done = true;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public void RunSynchronously_ShouldExecuteAsyncMethodReturningValueTaskWithResult()
    {
        int result = AsyncHelper.RunSynchronously(AsyncMethod);

        result.Should().Be(3);

        static async ValueTask<int> AsyncMethod()
        {
            await Task.Delay(50);
            return 3;
        }
    }

    [Fact]
    public void RunSynchronously_ShouldExecuteSyncMethodReturningValueTaskWithResult()
    {
        int result = AsyncHelper.RunSynchronously(SyncMethod);

        result.Should().Be(3);

        static ValueTask<int> SyncMethod()
        {
            return new ValueTask<int>(3);
        }
    }

    [Fact]
    public void RunSynchronously_ShouldRethrow_WhenAsyncMethodReturningValueTaskThrows()
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
    public void RunSynchronously_ShouldRethrow_WhenSyncMethodReturningValueTaskThrows()
    {
        Action act = () => AsyncHelper.RunSynchronously(AsyncMethod);

        act.Should().Throw<NotSupportedException>();

        static ValueTask AsyncMethod()
        {
            throw new NotSupportedException("test");
        }
    }

    [Fact]
    public void RunSynchronously_ShouldRethrow_WhenAsyncMethodReturningValueTaskWithResultThrows()
    {
        Action act = () => AsyncHelper.RunSynchronously(AsyncMethod);

        act.Should().Throw<NotSupportedException>();

        static async ValueTask<int> AsyncMethod()
        {
            await Task.Delay(50);
            throw new NotSupportedException("test");
        }
    }

    [Fact]
    public void RunSynchronously_ShouldRethrow_WhenSyncMethodReturningValueTaskWithResultThrows()
    {
        Action act = () => AsyncHelper.RunSynchronously(SyncMethod);

        act.Should().Throw<NotSupportedException>();

        static ValueTask<int> SyncMethod()
        {
            throw new NotSupportedException("test");
        }
    }
}
