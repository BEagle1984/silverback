// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Silverback.Messaging.Subscribers.Subscriptions;

internal static class DelegateSubscriber
{
    public static object Create<TMessage>(Action<TMessage> handler) =>
        new SyncSubscriber<TMessage>(handler);

    public static object Create<TMessage, TResult>(Func<TMessage, TResult> handler) =>
        new SyncSubscriberWithResult<TMessage, TResult>(handler);

    public static object Create<TMessage>(Func<TMessage, Task> handler) =>
        new AsyncSubscriber<TMessage>(handler);

    public static object Create<TMessage, TResult>(Func<TMessage, Task<TResult>> handler) =>
        new AsyncSubscriberWithResult<TMessage, TResult>(handler);

    public static object Create<TMessage>(Func<TMessage, ValueTask> handler) =>
        new SyncOrAsyncSubscriber<TMessage>(handler);

    public static object Create<TMessage, TResult>(Func<TMessage, ValueTask<TResult>> handler) =>
        new SyncOrAsyncSubscriberWithResult<TMessage, TResult>(handler);

    public static object Create<TMessage, T2>(Action<TMessage, T2> handler) =>
        new SyncSubscriberT2<TMessage, T2>(handler);

    public static object Create<TMessage, T2, TResult>(Func<TMessage, T2, TResult> handler) =>
        new SyncSubscriberWithResultT2<TMessage, T2, TResult>(handler);

    public static object Create<TMessage, T2>(Func<TMessage, T2, Task> handler) =>
        new AsyncSubscriberT2<TMessage, T2>(handler);

    public static object Create<TMessage, T2, TResult>(Func<TMessage, T2, Task<TResult>> handler) =>
        new AsyncSubscriberWithResultT2<TMessage, T2, TResult>(handler);

    public static object Create<TMessage, T2>(Func<TMessage, T2, ValueTask> handler) =>
        new SyncOrAsyncSubscriberT2<TMessage, T2>(handler);

    public static object Create<TMessage, T2, TResult>(Func<TMessage, T2, ValueTask<TResult>> handler) =>
        new SyncOrAsyncSubscriberWithResultT2<TMessage, T2, TResult>(handler);

    public static object Create<TMessage, T2, T3>(Action<TMessage, T2, T3> handler) =>
        new SyncSubscriberT3<TMessage, T2, T3>(handler);

    public static object Create<TMessage, T2, T3, TResult>(Func<TMessage, T2, T3, TResult> handler) =>
        new SyncSubscriberWithResultT3<TMessage, T2, T3, TResult>(handler);

    public static object Create<TMessage, T2, T3>(Func<TMessage, T2, T3, Task> handler) =>
        new AsyncSubscriberT3<TMessage, T2, T3>(handler);

    public static object Create<TMessage, T2, T3, TResult>(Func<TMessage, T2, T3, Task<TResult>> handler) =>
        new AsyncSubscriberWithResultT3<TMessage, T2, T3, TResult>(handler);

    public static object Create<TMessage, T2, T3>(Func<TMessage, T2, T3, ValueTask> handler) => new
        SyncOrAsyncSubscriberT3<TMessage, T2, T3>(handler);

    public static object Create<TMessage, T2, T3, TResult>(Func<TMessage, T2, T3, ValueTask<TResult>> handler) =>
        new SyncOrAsyncSubscriberWithResultT3<TMessage, T2, T3, TResult>(handler);

    public static object Create<TMessage, T2, T3, T4>(Action<TMessage, T2, T3, T4> handler) =>
        new SyncSubscriberT4<TMessage, T2, T3, T4>(handler);

    public static object Create<TMessage, T2, T3, T4, TResult>(Func<TMessage, T2, T3, T4, TResult> handler) =>
        new SyncSubscriberWithResultT4<TMessage, T2, T3, T4, TResult>(handler);

    public static object Create<TMessage, T2, T3, T4>(Func<TMessage, T2, T3, T4, Task> handler) =>
        new AsyncSubscriberT4<TMessage, T2, T3, T4>(handler);

    public static object Create<TMessage, T2, T3, T4, TResult>(Func<TMessage, T2, T3, T4, Task<TResult>> handler) =>
        new AsyncSubscriberWithResultT4<TMessage, T2, T3, T4, TResult>(handler);

    public static object Create<TMessage, T2, T3, T4>(Func<TMessage, T2, T3, T4, ValueTask> handler) => new
        SyncOrAsyncSubscriberT4<TMessage, T2, T3, T4>(handler);

    public static object Create<TMessage, T2, T3, T4, TResult>(Func<TMessage, T2, T3, T4, ValueTask<TResult>> handler) =>
        new SyncOrAsyncSubscriberWithResultT4<TMessage, T2, T3, T4, TResult>(handler);

    private sealed class SyncSubscriber<TMessage>
    {
        private readonly Action<TMessage> _handler;

        public SyncSubscriber(Action<TMessage> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public void Execute(TMessage message) => _handler.Invoke(message);
    }

    private sealed class SyncSubscriberWithResult<TMessage, TResult>
    {
        private readonly Func<TMessage, TResult> _handler;

        public SyncSubscriberWithResult(Func<TMessage, TResult> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public TResult Execute(TMessage message) => _handler.Invoke(message);
    }

    private sealed class AsyncSubscriber<TMessage>
    {
        private readonly Func<TMessage, Task> _handler;

        public AsyncSubscriber(Func<TMessage, Task> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public Task ExecuteAsync(TMessage message) => _handler.Invoke(message);
    }

    private sealed class AsyncSubscriberWithResult<TMessage, TResult>
    {
        private readonly Func<TMessage, Task<TResult>> _handler;

        public AsyncSubscriberWithResult(Func<TMessage, Task<TResult>> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public Task<TResult> ExecuteAsync(TMessage message) => _handler.Invoke(message);
    }

    private sealed class SyncOrAsyncSubscriber<TMessage>
    {
        private readonly Func<TMessage, ValueTask> _handler;

        public SyncOrAsyncSubscriber(Func<TMessage, ValueTask> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public ValueTask ExecuteAsync(TMessage message) => _handler.Invoke(message);
    }

    private sealed class SyncOrAsyncSubscriberWithResult<TMessage, TResult>
    {
        private readonly Func<TMessage, ValueTask<TResult>> _handler;

        public SyncOrAsyncSubscriberWithResult(Func<TMessage, ValueTask<TResult>> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public ValueTask<TResult> ExecuteAsync(TMessage message) => _handler.Invoke(message);
    }

    private sealed class SyncSubscriberT2<TMessage, T2>
    {
        private readonly Action<TMessage, T2> _handler;

        public SyncSubscriberT2(Action<TMessage, T2> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public void Execute(TMessage message, T2 t2) => _handler.Invoke(message, t2);
    }

    private sealed class SyncSubscriberWithResultT2<TMessage, T2, TResult>
    {
        private readonly Func<TMessage, T2, TResult> _handler;

        public SyncSubscriberWithResultT2(Func<TMessage, T2, TResult> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public TResult Execute(TMessage message, T2 t2) => _handler.Invoke(message, t2);
    }

    private sealed class AsyncSubscriberT2<TMessage, T2>
    {
        private readonly Func<TMessage, T2, Task> _handler;

        public AsyncSubscriberT2(Func<TMessage, T2, Task> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public Task ExecuteAsync(TMessage message, T2 t2) => _handler.Invoke(message, t2);
    }

    private sealed class AsyncSubscriberWithResultT2<TMessage, T2, TResult>
    {
        private readonly Func<TMessage, T2, Task<TResult>> _handler;

        public AsyncSubscriberWithResultT2(Func<TMessage, T2, Task<TResult>> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public Task<TResult> ExecuteAsync(TMessage message, T2 t2) => _handler.Invoke(message, t2);
    }

    private sealed class SyncOrAsyncSubscriberT2<TMessage, T2>
    {
        private readonly Func<TMessage, T2, ValueTask> _handler;

        public SyncOrAsyncSubscriberT2(Func<TMessage, T2, ValueTask> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public ValueTask ExecuteAsync(TMessage message, T2 t2) => _handler.Invoke(message, t2);
    }

    private sealed class SyncOrAsyncSubscriberWithResultT2<TMessage, T2, TResult>
    {
        private readonly Func<TMessage, T2, ValueTask<TResult>> _handler;

        public SyncOrAsyncSubscriberWithResultT2(Func<TMessage, T2, ValueTask<TResult>> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public ValueTask<TResult> ExecuteAsync(TMessage message, T2 t2) => _handler.Invoke(message, t2);
    }

    private sealed class SyncSubscriberT3<TMessage, T2, T3>
    {
        private readonly Action<TMessage, T2, T3> _handler;

        public SyncSubscriberT3(Action<TMessage, T2, T3> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public void Execute(TMessage message, T2 t2, T3 t3) => _handler.Invoke(message, t2, t3);
    }

    private sealed class SyncSubscriberWithResultT3<TMessage, T2, T3, TResult>
    {
        private readonly Func<TMessage, T2, T3, TResult> _handler;

        public SyncSubscriberWithResultT3(Func<TMessage, T2, T3, TResult> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public TResult Execute(TMessage message, T2 t2, T3 t3) => _handler.Invoke(message, t2, t3);
    }

    private sealed class AsyncSubscriberT3<TMessage, T2, T3>
    {
        private readonly Func<TMessage, T2, T3, Task> _handler;

        public AsyncSubscriberT3(Func<TMessage, T2, T3, Task> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public Task ExecuteAsync(TMessage message, T2 t2, T3 t3) => _handler.Invoke(message, t2, t3);
    }

    private sealed class AsyncSubscriberWithResultT3<TMessage, T2, T3, TResult>
    {
        private readonly Func<TMessage, T2, T3, Task<TResult>> _handler;

        public AsyncSubscriberWithResultT3(Func<TMessage, T2, T3, Task<TResult>> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public Task<TResult> ExecuteAsync(TMessage message, T2 t2, T3 t3) => _handler.Invoke(message, t2, t3);
    }

    private sealed class SyncOrAsyncSubscriberT3<TMessage, T2, T3>
    {
        private readonly Func<TMessage, T2, T3, ValueTask> _handler;

        public SyncOrAsyncSubscriberT3(Func<TMessage, T2, T3, ValueTask> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public ValueTask ExecuteAsync(TMessage message, T2 t2, T3 t3) => _handler.Invoke(message, t2, t3);
    }

    private sealed class SyncOrAsyncSubscriberWithResultT3<TMessage, T2, T3, TResult>
    {
        private readonly Func<TMessage, T2, T3, ValueTask<TResult>> _handler;

        public SyncOrAsyncSubscriberWithResultT3(Func<TMessage, T2, T3, ValueTask<TResult>> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public ValueTask<TResult> ExecuteAsync(TMessage message, T2 t2, T3 t3) => _handler.Invoke(message, t2, t3);
    }

    private sealed class SyncSubscriberT4<TMessage, T2, T3, T4>
    {
        private readonly Action<TMessage, T2, T3, T4> _handler;

        public SyncSubscriberT4(Action<TMessage, T2, T3, T4> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public void Execute(TMessage message, T2 t2, T3 t3, T4 t4) => _handler.Invoke(message, t2, t3, t4);
    }

    private sealed class SyncSubscriberWithResultT4<TMessage, T2, T3, T4, TResult>
    {
        private readonly Func<TMessage, T2, T3, T4, TResult> _handler;

        public SyncSubscriberWithResultT4(Func<TMessage, T2, T3, T4, TResult> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public TResult Execute(TMessage message, T2 t2, T3 t3, T4 t4) => _handler.Invoke(message, t2, t3, t4);
    }

    private sealed class AsyncSubscriberT4<TMessage, T2, T3, T4>
    {
        private readonly Func<TMessage, T2, T3, T4, Task> _handler;

        public AsyncSubscriberT4(Func<TMessage, T2, T3, T4, Task> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public Task ExecuteAsync(TMessage message, T2 t2, T3 t3, T4 t4) => _handler.Invoke(message, t2, t3, t4);
    }

    private sealed class AsyncSubscriberWithResultT4<TMessage, T2, T3, T4, TResult>
    {
        private readonly Func<TMessage, T2, T3, T4, Task<TResult>> _handler;

        public AsyncSubscriberWithResultT4(Func<TMessage, T2, T3, T4, Task<TResult>> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public Task<TResult> ExecuteAsync(TMessage message, T2 t2, T3 t3, T4 t4) => _handler.Invoke(message, t2, t3, t4);
    }

    private sealed class SyncOrAsyncSubscriberT4<TMessage, T2, T3, T4>
    {
        private readonly Func<TMessage, T2, T3, T4, ValueTask> _handler;

        public SyncOrAsyncSubscriberT4(Func<TMessage, T2, T3, T4, ValueTask> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public ValueTask ExecuteAsync(TMessage message, T2 t2, T3 t3, T4 t4) => _handler.Invoke(message, t2, t3, t4);
    }

    private sealed class SyncOrAsyncSubscriberWithResultT4<TMessage, T2, T3, T4, TResult>
    {
        private readonly Func<TMessage, T2, T3, T4, ValueTask<TResult>> _handler;

        public SyncOrAsyncSubscriberWithResultT4(Func<TMessage, T2, T3, T4, ValueTask<TResult>> handler)
        {
            _handler = handler;
        }

        [UsedImplicitly]
        public ValueTask<TResult> ExecuteAsync(TMessage message, T2 t2, T3 t3, T4 t4) => _handler.Invoke(message, t2, t3, t4);
    }
}
