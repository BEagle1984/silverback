// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

internal sealed class LazyMessageStreamEnumerable<TMessage>
    : ILazyMessageStreamEnumerable<TMessage>, ILazyMessageStreamEnumerable, ILazyArgumentValue, IDisposable
{
    private readonly TaskCompletionSource<IMessageStreamEnumerable> _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly System.Threading.Lock _syncRoot = new();

    private MessageStreamEnumerable<TMessage>? _stream;

    private bool _isCreationCanceled;

    private bool _isDisposed;

    public LazyMessageStreamEnumerable(IReadOnlyCollection<IMessageFilter>? filters = null)
    {
        Filters = filters;
    }

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.MessageType" />
    public Type MessageType => typeof(TMessage);

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.Stream" />
    public IMessageStreamEnumerable<TMessage>? Stream
    {
        get
        {
            lock (_syncRoot)
            {
                return _stream;
            }
        }
    }

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.Filters" />
    public IReadOnlyCollection<IMessageFilter>? Filters { get; }

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.Stream" />
    IMessageStreamEnumerable? ILazyMessageStreamEnumerable.Stream => (IMessageStreamEnumerable?)Stream;

    object? ILazyArgumentValue.Value => Stream;

    /// <inheritdoc cref="ILazyMessageStreamEnumerable{TMessage}.WaitUntilCreatedAsync" />
    public Task WaitUntilCreatedAsync() => _taskCompletionSource.Task;

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.GetOrCreateStream" />
    public IMessageStreamEnumerable GetOrCreateStream()
    {
        lock (_syncRoot)
        {
            Check.ThrowObjectDisposedIf(_isDisposed, this);

            if (_isCreationCanceled)
                throw new OperationCanceledException("The stream creation was canceled.");

            if (_stream == null)
            {
                _stream = new MessageStreamEnumerable<TMessage>();
                _taskCompletionSource.SetResult(_stream);
            }

            return _stream;
        }
    }

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.Abort" />
    public void Abort()
    {
        MessageStreamEnumerable<TMessage>? stream;

        lock (_syncRoot)
        {
            stream = _stream;

            if (stream == null)
            {
                CancelCreation();
                return;
            }
        }

        stream.Abort();
    }

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.CompleteAsync" />
    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        MessageStreamEnumerable<TMessage>? stream;

        lock (_syncRoot)
        {
            stream = _stream;

            if (stream == null)
            {
                CancelCreation();
                return Task.CompletedTask;
            }
        }

        return stream.CompleteAsync(cancellationToken);
    }

    public void Dispose()
    {
        MessageStreamEnumerable<TMessage>? stream;

        lock (_syncRoot)
        {
            if (_isDisposed)
                return;

            stream = _stream;
            _isDisposed = true;
        }

        stream?.Dispose();
    }

    private void CancelCreation()
    {
        _isCreationCanceled = true;
        _taskCompletionSource.TrySetCanceled();
    }
}
