// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class EnumerableDisposeAllExtensionsFixture
{
    private interface IMustDispose
    {
        public bool Disposed { get; }
    }

    [Fact]
    public async Task DisposeAllAsync_ShouldDisposeAllDisposableObjects()
    {
        object[] objects = { new Disposable(), new NotDisposable(), new AsyncDisposable(), new AsyncAndSyncDisposable() };

        await objects.DisposeAllAsync();

        objects[0].As<IMustDispose>().Disposed.Should().BeTrue();
        objects[2].As<IMustDispose>().Disposed.Should().BeTrue();
        objects[3].As<IMustDispose>().Disposed.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAllAsync_ShouldDisposeObjectsOnce_WhenBothSyncAndAsyncDisposableAreImplemented()
    {
        AsyncAndSyncDisposable[] objects = { new(), new() };

        await objects.DisposeAllAsync();

        objects[0].AsyncDisposed.Should().BeTrue();
        objects[0].SyncDisposed.Should().BeFalse();
        objects[1].AsyncDisposed.Should().BeTrue();
        objects[1].SyncDisposed.Should().BeFalse();
    }

    private class Disposable : IDisposable, IMustDispose
    {
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    private class AsyncDisposable : IAsyncDisposable, IMustDispose
    {
        public bool Disposed { get; set; }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private class AsyncAndSyncDisposable : IDisposable, IAsyncDisposable, IMustDispose
    {
        public bool Disposed => SyncDisposed || AsyncDisposed;

        public bool SyncDisposed { get; set; }

        public bool AsyncDisposed { get; set; }

        public void Dispose()
        {
            SyncDisposed = true;
        }

        public ValueTask DisposeAsync()
        {
            AsyncDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private class NotDisposable
    {
    }
}
