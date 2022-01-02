// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Core.Lock;

public class NullLockFixture
{
    [Fact]
    public void Instance_ShouldReturnStaticInstance()
    {
        NullLock nullLock1 = NullLock.Instance;
        NullLock nullLock2 = NullLock.Instance;

        nullLock1.Should().NotBeNull();
        nullLock1.Should().BeSameAs(nullLock2);
    }

    [Fact]
    public async Task AcquireCoreAsync_ShouldReturnHandle()
    {
        DistributedLockHandle handle = await NullLock.Instance.AcquireAsync();

        handle.Should().NotBeNull();
    }

    [Fact]
    public async Task AcquireCoreAsync_ShouldReturnDisposableHandle()
    {
        DistributedLockHandle handle = await NullLock.Instance.AcquireAsync();

        Action act = () => handle.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AcquireCoreAsync_ShouldReturnAsyncDisposableHandle()
    {
        DistributedLockHandle handle = await NullLock.Instance.AcquireAsync();

        Func<Task> act = () => handle.DisposeAsync().AsTask();

        await act.Should().NotThrowAsync();
    }
}
