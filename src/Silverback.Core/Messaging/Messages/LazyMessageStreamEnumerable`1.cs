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

    private readonly Func<ILazyMessageStreamEnumerable, bool, bool, ILazyMessageStreamEnumerable?>? _onSubscriberCompleted;

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
    ///     The callback invoked when the subscriber of the created stream returns or throws, with this lazy stream, a value indicating
    ///     whether the subscriber threw and a value indicating whether it returned or threw because the cancellation token passed to it was
    ///     canceled. It returns the lazy stream replacing this one, if the subscriber has to be invoked again with it, otherwise
    ///     <c>null</c>.
    /// </param>
    public LazyMessageStreamEnumerable(
        IReadOnlyCollection<IMessageFilter>? filters = null,
        Func<ILazyMessageStreamEnumerable, bool, bool, ILazyMessageStreamEnumerable?>? onSubscriberCompleted = null)
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

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.Replacement" />
    /// <remarks>
    ///     Set by the provider holding its lock before the stream is detached, which orders the read by the refused push after the write.
    /// </remarks>
    public ILazyMessageStreamEnumerable? Replacement { get; private set; }

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

    /// <inheritdoc cref="ILazyMessageStreamEnumerable.CreateReplacement" />
    public ILazyMessageStreamEnumerable CreateReplacement()
    {
        LazyMessageStreamEnumerable<TMessage> replacement = new(Filters, _onSubscriberCompleted);
        Replacement = replacement;
        return replacement;
    }

    /// <inheritdoc cref="ILazyStreamArgumentValue.NotifySubscriberCompleted" />
    public ILazyStreamArgumentValue? NotifySubscriberCompleted(bool hasThrown, bool isCanceled)
    {
        MessageStreamEnumerable<TMessage>? stream;

        lock (_syncRoot)
        {
            stream = _stream;

            if (stream != null && !hasThrown)
                HasSubscriberReturned = true;
        }

        if (stream == null)
            return null;

        stream.Abandon();

        ILazyMessageStreamEnumerable? replacement = _onSubscriberCompleted?.Invoke(this, hasThrown, isCanceled);

        // Detach only once the replacement is set, since the refused push is then pushed to it
        if (replacement != null)
            stream.Detach();

        return (ILazyStreamArgumentValue?)replacement;
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
