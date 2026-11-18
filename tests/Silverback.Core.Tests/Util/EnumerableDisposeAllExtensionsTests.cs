// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class EnumerableDisposeAllExtensionsTests
{
    private interface IMustDispose
    {
        bool IsDisposed { get; }
    }

    [Fact]
    public void DisposeAll_ShouldDisposeAllDisposableObjects()
    {
        object[] objects = [new Disposable(), new NotDisposable(), new AsyncDisposable(), new AsyncAndSyncDisposable()];

        objects.DisposeAll();

        objects[0].ShouldBeAssignableTo<IMustDispose>().IsDisposed.ShouldBeTrue();
        objects[2].ShouldBeAssignableTo<IMustDispose>().IsDisposed.ShouldBeTrue();
        objects[3].ShouldBeAssignableTo<IMustDispose>().IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public void DisposeAll_ShouldDisposeObjectsOnce_WhenBothSyncAndAsyncDisposableAreImplemented()
    {
        AsyncAndSyncDisposable[] objects = [new(), new()];

        objects.DisposeAll();

        objects[0].SyncDisposed.ShouldBeTrue();
        objects[0].AsyncDisposed.ShouldBeFalse();
        objects[1].SyncDisposed.ShouldBeTrue();
        objects[1].AsyncDisposed.ShouldBeFalse();
    }

    [Fact]
    public void DisposeAll_ShouldDisposeAllObjectsAndRethrow_WhenDisposeFails()
    {
        InvalidOperationException exception = new("test");
        object[] objects = [new Disposable(), new ThrowingDisposable(exception), new AsyncDisposable()];

        Action act = objects.DisposeAll;

        act.ShouldThrow<InvalidOperationException>().ShouldBeSameAs(exception);
        objects[0].ShouldBeAssignableTo<IMustDispose>().IsDisposed.ShouldBeTrue();
        objects[2].ShouldBeAssignableTo<IMustDispose>().IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public void DisposeAll_ShouldThrowAggregateException_WhenMultipleDisposeFail()
    {
        InvalidOperationException exception1 = new("test1");
        InvalidOperationException exception2 = new("test2");
        object[] objects = [new ThrowingDisposable(exception1), new Disposable(), new ThrowingDisposable(exception2)];

        Action act = objects.DisposeAll;

        act.ShouldThrow<AggregateException>().InnerExceptions.ShouldBe([exception1, exception2]);
        objects[1].ShouldBeAssignableTo<IMustDispose>().IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public async Task DisposeAllAsync_ShouldDisposeAllDisposableObjects()
    {
        object[] objects = [new Disposable(), new NotDisposable(), new AsyncDisposable(), new AsyncAndSyncDisposable()];

        await objects.DisposeAllAsync();

        objects[0].ShouldBeAssignableTo<IMustDispose>().IsDisposed.ShouldBeTrue();
        objects[2].ShouldBeAssignableTo<IMustDispose>().IsDisposed.ShouldBeTrue();
        objects[3].ShouldBeAssignableTo<IMustDispose>().IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public async Task DisposeAllAsync_ShouldDisposeObjectsOnce_WhenBothSyncAndAsyncDisposableAreImplemented()
    {
        AsyncAndSyncDisposable[] objects = [new(), new()];

        await objects.DisposeAllAsync();

        objects[0].AsyncDisposed.ShouldBeTrue();
        objects[0].SyncDisposed.ShouldBeFalse();
        objects[1].AsyncDisposed.ShouldBeTrue();
        objects[1].SyncDisposed.ShouldBeFalse();
    }

    [Fact]
    public async Task DisposeAllAsync_ShouldDisposeAllObjectsAndRethrow_WhenDisposeFails()
    {
        InvalidOperationException exception = new("test");
        object[] objects = [new Disposable(), new ThrowingDisposable(exception), new AsyncDisposable()];

        Func<Task> act = () => objects.DisposeAllAsync().AsTask();

        (await act.ShouldThrowAsync<InvalidOperationException>()).ShouldBeSameAs(exception);
        objects[0].ShouldBeAssignableTo<IMustDispose>().IsDisposed.ShouldBeTrue();
        objects[2].ShouldBeAssignableTo<IMustDispose>().IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public async Task DisposeAllAsync_ShouldThrowAggregateException_WhenMultipleDisposeFail()
    {
        InvalidOperationException exception1 = new("test1");
        InvalidOperationException exception2 = new("test2");
        object[] objects = [new ThrowingDisposable(exception1), new AsyncDisposable(), new ThrowingDisposable(exception2)];

        Func<Task> act = () => objects.DisposeAllAsync().AsTask();

        (await act.ShouldThrowAsync<AggregateException>()).InnerExceptions.ShouldBe([exception1, exception2]);
        objects[1].ShouldBeAssignableTo<IMustDispose>().IsDisposed.ShouldBeTrue();
    }

    private sealed class ThrowingDisposable : IDisposable
    {
        private readonly Exception _exception;

        public ThrowingDisposable(Exception exception)
        {
            _exception = exception;
        }

        public void Dispose() => throw _exception;
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
