// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

internal sealed class LazyMessageStreamEnumerable<TMessage>
    : ILazyMessageStreamEnumerable<TMessage>, ILazyMessageStreamEnumerable, ILazyArgumentValue, IDisposable
{
    private readonly TaskCompletionSource<IMessageStreamEnumerable> _taskCompletionSource = new();

    private MessageStreamEnumerable<TMessage>? _stream;

    private bool _isDisposed;

    public LazyMessageStreamEnumerable(IReadOnlyCollection<IMessageFilter>? filters = null)
    {
        Filters = filters;
    }

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.MessageType" />
    public Type MessageType => typeof(TMessage);

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.Stream" />
    public IMessageStreamEnumerable<TMessage>? Stream => _stream;

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.Filters" />
    public IReadOnlyCollection<IMessageFilter>? Filters { get; }

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.Stream" />
    IMessageStreamEnumerable? ILazyMessageStreamEnumerable.Stream => _stream;

    object? ILazyArgumentValue.Value => Stream;

    /// <inheritdoc cref="ILazyMessageStreamEnumerable{TMessage}.WaitUntilCreatedAsync" />
    public Task WaitUntilCreatedAsync() => _taskCompletionSource.Task;

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.GetOrCreateStream" />
    public IMessageStreamEnumerable GetOrCreateStream()
    {
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        if (_stream == null)
        {
            _stream = new MessageStreamEnumerable<TMessage>();
            _taskCompletionSource.SetResult(_stream);
        }

        return _stream;
    }

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.Cancel" />
    public void Cancel() => _taskCompletionSource.SetCanceled();

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _stream?.Dispose();
        _isDisposed = true;
    }
}
