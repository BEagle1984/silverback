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
    : ILazyMessageStreamEnumerable<TMessage>, ILazyMessageStreamEnumerable, ILazyStreamArgumentValue, IDisposable
{
    private readonly TaskCompletionSource<IMessageStreamEnumerable> _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly System.Threading.Lock _syncRoot = new();

    private readonly Func<bool, bool, bool>? _onSubscriberCompleted;

    private MessageStreamEnumerable<TMessage>? _stream;

    private bool _isCreationCanceled;

    private bool _isDisposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LazyMessageStreamEnumerable{TMessage}" /> class.
    /// </summary>
    /// <param name="filters">
    ///     The filters to be applied.
    /// </param>
    /// <param name="onSubscriberCompleted">
    ///     The callback invoked when the subscriber of the created stream returns or throws, with a value indicating whether the subscriber
    ///     threw and a value indicating whether it returned or threw because the cancellation token passed to it was canceled. It returns a
    ///     value indicating whether the stream must be detached, because the subscribers of the other streams are still enumerating them
    ///     (see <see cref="MessageStreamEnumerable{TMessage}.Detach" />).
    /// </param>
    public LazyMessageStreamEnumerable(IReadOnlyCollection<IMessageFilter>? filters = null, Func<bool, bool, bool>? onSubscriberCompleted = null)
    {
        Filters = filters;
        _onSubscriberCompleted = onSubscriberCompleted;
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

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.HasSubscriberReturned" />
    /// <remarks>
    ///     The provider reads it while holding its lock, which the notifying subscriber acquires after setting it.
    /// </remarks>
    public bool HasSubscriberReturned { get; private set; }

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.Stream" />
    IMessageStreamEnumerable? ILazyMessageStreamEnumerable.Stream => (IMessageStreamEnumerable?)Stream;

    object? ILazyArgumentValue.Value => Stream;

    /// <inheritdoc cref="ILazyMessageStreamEnumerable{TMessage}.WaitUntilCreatedAsync" />
    public Task WaitUntilCreatedAsync() => _taskCompletionSource.Task;

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.GetOrCreateStream" />
    public IMessageStreamEnumerable GetOrCreateStream()
    {
        IMessageStreamEnumerable stream = GetOrCreateStreamWithoutSignaling(out _);
        SignalCreated();
        return stream;
    }

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.GetOrCreateStreamWithoutSignaling" />
    public IMessageStreamEnumerable GetOrCreateStreamWithoutSignaling(out bool isCreated)
    {
        lock (_syncRoot)
        {
            Check.ThrowObjectDisposedIf(_isDisposed, this);

            if (_isCreationCanceled)
                throw new OperationCanceledException("The stream creation was canceled.");

            isCreated = _stream == null;
            _stream ??= new MessageStreamEnumerable<TMessage>();
            return _stream;
        }
    }

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.SignalCreated" />
    public void SignalCreated()
    {
        MessageStreamEnumerable<TMessage>? stream;

        lock (_syncRoot)
        {
            stream = _stream;
        }

        // Once created, the stream is never replaced and its creation can't be canceled anymore (see CancelCreation)
        if (stream != null)
            _taskCompletionSource.TrySetResult(stream);
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

    /// <inheritdoc cref="ILazyStreamArgumentValue.NotifySubscriberCompleted" />
    public void NotifySubscriberCompleted(bool hasThrown, bool isCanceled)
    {
        MessageStreamEnumerable<TMessage>? stream;

        lock (_syncRoot)
        {
            stream = _stream;

            if (stream != null && !hasThrown)
                HasSubscriberReturned = true;
        }

        if (stream == null)
            return;

        stream.Abandon();

        if (_onSubscriberCompleted?.Invoke(hasThrown, isCanceled) == true)
            stream.Detach();
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
