// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public partial class TaskExtensionsFixture
{
    [Fact]
    public void SafeWait_ShouldExecuteAsyncMethodReturningTask()
    {
        bool done = false;

        AsyncMethod().SafeWait();

        done.Should().BeTrue();

        async Task AsyncMethod()
        {
            await Task.Delay(50);
            done = true;
        }
    }

    [Fact]
    public void SafeWait_ShouldExecuteAsyncMethodReturningTaskWithResult()
    {
        int result = AsyncMethod().SafeWait();

        result.Should().Be(3);

        static async Task<int> AsyncMethod()
        {
            await Task.Delay(50);
            return 3;
        }
    }

    [Fact]
    public void SafeWait_ShouldRethrow_WhenAsyncMethodReturningTaskThrows()
    {
        Action act = () => AsyncMethod().SafeWait();

        act.Should().Throw<NotSupportedException>();

        static async Task AsyncMethod()
        {
            await Task.Delay(50);
            throw new NotSupportedException("test");
        }
    }

    [Fact]
    public void SafeWait_ShouldRethrow_WhenAsyncMethodReturningTaskWithResultThrows()
    {
        Action act = () => AsyncMethod().SafeWait();

        act.Should().Throw<NotSupportedException>();

        static async Task<int> AsyncMethod()
        {
            await Task.Delay(50);
            throw new NotSupportedException("test");
        }
    }

    [Fact]
    public void SafeWait_ShouldExecuteAsyncMethodReturningValueTask()
    {
        bool done = false;

        AsyncMethod().SafeWait();

        done.Should().BeTrue();

        async ValueTask AsyncMethod()
        {
            await Task.Delay(50);
            done = true;
        }
    }

    [Fact]
    public void SafeWait_ShouldExecuteSyncMethodReturningValueTask()
    {
        bool done;

        SyncMethod().SafeWait();

        done.Should().BeTrue();

        ValueTask SyncMethod()
        {
            done = true;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public void SafeWait_ShouldExecuteAsyncMethodReturningValueTaskWithResult()
    {
        int result = AsyncMethod().SafeWait();

        result.Should().Be(3);

        static async ValueTask<int> AsyncMethod()
        {
            await Task.Delay(50);
            return 3;
        }
    }

    [Fact]
    public void SafeWait_ShouldExecuteSyncMethodReturningValueTaskWithResult()
    {
        int result = SyncMethod().SafeWait();

        result.Should().Be(3);

        static ValueTask<int> SyncMethod() => new(3);
    }

    [Fact]
    public void SafeWait_ShouldRethrow_WhenAsyncMethodReturningValueTaskThrows()
    {
        Action act = () => AsyncMethod().SafeWait();

        act.Should().Throw<NotSupportedException>();

        static async ValueTask AsyncMethod()
        {
            await Task.Delay(50);
            throw new NotSupportedException("test");
        }
    }

    [Fact]
    public void SafeWait_ShouldRethrow_WhenSyncMethodReturningValueTaskThrows()
    {
        Action act = () => AsyncMethod().SafeWait();

        act.Should().Throw<NotSupportedException>();

        static ValueTask AsyncMethod() => throw new NotSupportedException("test");
    }

    [Fact]
    public void SafeWait_ShouldRethrow_WhenAsyncMethodReturningValueTaskWithResultThrows()
    {
        Action act = () => AsyncMethod().SafeWait();

        act.Should().Throw<NotSupportedException>();

        static async ValueTask<int> AsyncMethod()
        {
            await Task.Delay(50);
            throw new NotSupportedException("test");
        }
    }

    [Fact]
    public void SafeWait_ShouldRethrow_WhenSyncMethodReturningValueTaskWithResultThrows()
    {
        Action act = () => SyncMethod().SafeWait();

        act.Should().Throw<NotSupportedException>();

        static ValueTask<int> SyncMethod() => throw new NotSupportedException("test");
    }
}
