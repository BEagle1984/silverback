// Copyright (c) 2024 Sergio Aquilini
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
        public bool IsDisposed { get; }
    }

    [Fact]
    public async Task DisposeAllAsync_ShouldDisposeAllDisposableObjects()
    {
        object[] objects = [new Disposable(), new NotDisposable(), new AsyncDisposable(), new AsyncAndSyncDisposable()];

        await objects.DisposeAllAsync();

        objects[0].As<IMustDispose>().IsDisposed.Should().BeTrue();
        objects[2].As<IMustDispose>().IsDisposed.Should().BeTrue();
        objects[3].As<IMustDispose>().IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAllAsync_ShouldDisposeObjectsOnce_WhenBothSyncAndAsyncDisposableAreImplemented()
    {
        AsyncAndSyncDisposable[] objects = [new AsyncAndSyncDisposable(), new AsyncAndSyncDisposable()];

        await objects.DisposeAllAsync();

        objects[0].AsyncDisposed.Should().BeTrue();
        objects[0].SyncDisposed.Should().BeFalse();
        objects[1].AsyncDisposed.Should().BeTrue();
        objects[1].SyncDisposed.Should().BeFalse();
    }

    private class Disposable : IDisposable, IMustDispose
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    private class AsyncDisposable : IAsyncDisposable, IMustDispose
    {
        public bool IsDisposed { get; set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class AsyncAndSyncDisposable : IDisposable, IAsyncDisposable, IMustDispose
    {
        public bool IsDisposed => SyncDisposed || AsyncDisposed;

        public bool SyncDisposed { get; set; }

        public bool AsyncDisposed { get; set; }

        public void Dispose() => SyncDisposed = true;

        public ValueTask DisposeAsync()
        {
            AsyncDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private class NotDisposable;
}
