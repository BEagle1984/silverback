// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Relays the streamed messages to all the linked <see cref="MessageStreamEnumerable{TMessage}" />.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages being streamed.
/// </typeparam>
internal sealed class MessageStreamProvider<TMessage> : MessageStreamProvider
{
    private readonly List<ILazyMessageStreamEnumerable> _lazyStreams = [];

    private readonly SemaphoreSlim _completeSemaphore = new(1, 1);

    private readonly TaskCompletionSource _subscribersReturnedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly bool _restartsReturningSubscribers;

    private MethodInfo? _genericCreateStreamMethodInfo;

    private bool _completed;

    private bool _aborted;

    private bool _disposed;

    private bool _isCompleting;

    private bool _isAborting;

    // Set holding the _lazyStreams lock when the streams are being aborted or completed: no replacement stream is created afterwards
    private bool _isClosed;

    // Set holding the _lazyStreams lock when a subscriber throws: no replacement stream is created afterwards
    private bool _hasSubscriberThrown;

    // Set holding the _lazyStreams lock when a subscriber throws an exception other than the cancellation (see OnSubscriberCompleted)
    private bool _hasSubscriberFailed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageStreamProvider{TMessage}" /> class.
    /// </summary>
    /// <param name="restartsReturningSubscribers">
    ///     A value indicating whether a subscriber that returns (e.g., with a <c>break</c>), while the subscribers of the other streams are
    ///     still enumerating them, is invoked again with a new stream, which receives the next messages. It isn't if the cancellation token
    ///     passed to it was canceled (e.g., because the consumer is stopping), nor once a subscriber threw. A subscriber returning or
    ///     throwing the cancellation because the token was canceled instead completes the <see cref="SubscribersReturnedTask" /> as if all
    ///     the subscribers returned, unless another subscriber failed (or, when throwing, it's the only subscriber). Otherwise, its pending
    ///     push is left to the completion or the abort,
    ///     which are expected once all the subscribers returned (see <see cref="SubscribersReturnedTask" />).
    /// </param>
    public MessageStreamProvider(bool restartsReturningSubscribers = false)
    {
        _restartsReturningSubscribers = restartsReturningSubscribers;
    }

    /// <inheritdoc cref="MessageStreamProvider.MessageType" />
    public override Type MessageType => typeof(TMessage);

    /// <inheritdoc cref="MessageStreamProvider.StreamsCount" />
    public override int StreamsCount => _lazyStreams.Count;

    /// <inheritdoc cref="MessageStreamProvider.SubscribersReturnedTask" />
    public override Task SubscribersReturnedTask => _subscribersReturnedTaskCompletionSource.Task;

    // The streams were completed or aborted, which makes the push fail (also before the owner, e.g. the sequence, is aborted)
    internal bool IsCompletedOrAborted => _completed || _aborted;

    /// <summary>
    ///     Adds the specified message to the stream. The returned <see cref="Task" /> will complete only when the message has actually been
    ///     pulled and processed.
    /// </summary>
    /// <param name="message">
    ///     The message to be added.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <param name="onPullAction">
    ///     An action to be executed when the message is pulled.
    /// </param>
    /// <param name="onPullActionArgument">
    ///     The argument to be passed to the <paramref name="onPullAction" />.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task will complete only
    ///     when the message has actually been pulled and processed and its result contains the number of
    ///     <see cref="IMessageStreamEnumerable{TMessage}" /> that have been pushed.
    /// </returns>
    [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Global", Justification = "False positive")]
    public async Task<int> PushAsync(
        TMessage message,
        bool throwIfUnhandled = true,
        Action<object?>? onPullAction = null,
        object? onPullActionArgument = null,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(message, nameof(message));

        if (_completed || _aborted)
            throw new InvalidOperationException("The streams are already completed or aborted.");

        List<Task> processingTasks = [.. PushToCompatibleStreams(message, onPullAction, onPullActionArgument, cancellationToken)];

        if (processingTasks.Count > 0)
            await Task.WhenAll(processingTasks).ConfigureAwait(false);
        else if (throwIfUnhandled)
            throw new UnhandledMessageException(message!);

        return processingTasks.Count;
    }

    /// <inheritdoc cref="MessageStreamProvider.AbortIfPending" />
    public override void AbortIfPending()
    {
        if (!_completed)
            Abort();
    }

    /// <inheritdoc cref="MessageStreamProvider.Abort" />
    public override void Abort()
    {
        if (_isAborting || _isCompleting) // prevent deadlocking on reentry (it's a very optimistic approach, could harden later)
            return;

        _completeSemaphore.Wait();

        try
        {
            if (_completed)
                throw new InvalidOperationException("The streams are already completed.");

            if (_aborted)
                return;

            _isAborting = true;

            foreach (ILazyMessageStreamEnumerable lazyStream in CloseAndGetLazyStreams())
            {
                lazyStream.Abort();
            }

            _aborted = true;
        }
        finally
        {
            _isAborting = false;
            _completeSemaphore.Release();
        }
    }

    /// <inheritdoc cref="MessageStreamProvider.CompleteAsync" />
    public override async ValueTask CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (_isCompleting || _isAborting) // prevent deadlocking on reentry (it's a very optimistic approach, could harden later)
            return;

        await _completeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_aborted)
                throw new InvalidOperationException("The stream are already aborted.");

            if (_completed)
                return;

            _isCompleting = true;

            await CloseAndGetLazyStreams()
                .ParallelForEachAsync(async lazyStream => await lazyStream.CompleteAsync(cancellationToken).ConfigureAwait(false))
                .ConfigureAwait(false);

            _completed = true;
        }
        finally
        {
            _isCompleting = false;
            _completeSemaphore.Release();
        }
    }

    /// <inheritdoc cref="IMessageStreamProvider.CreateStream" />
    public override IMessageStreamEnumerable<object> CreateStream(
        Type messageType,
        IReadOnlyCollection<IMessageFilter>? filters = null)
    {
        ILazyMessageStreamEnumerable lazyStream = (ILazyMessageStreamEnumerable)CreateLazyStream(messageType, filters);
        return (IMessageStreamEnumerable<object>)lazyStream.GetOrCreateStream();
    }

    /// <inheritdoc cref="IMessageStreamProvider.CreateStream{TMessage}" />
    public override IMessageStreamEnumerable<TMessageLinked> CreateStream<TMessageLinked>(IReadOnlyCollection<IMessageFilter>? filters = null)
    {
        ILazyMessageStreamEnumerable lazyStream = (ILazyMessageStreamEnumerable)CreateLazyStream<TMessageLinked>(filters);
        return (IMessageStreamEnumerable<TMessageLinked>)lazyStream.GetOrCreateStream();
    }

    /// <inheritdoc cref="IMessageStreamProvider.CreateLazyStream" />
    public override ILazyMessageStreamEnumerable<object> CreateLazyStream(
        Type messageType,
        IReadOnlyCollection<IMessageFilter>? filters = null)
    {
        _genericCreateStreamMethodInfo ??= GetType().GetMethod(
            nameof(CreateLazyStream),
            1,
            [typeof(IReadOnlyCollection<IMessageFilter>)])!;

        object lazyStream = _genericCreateStreamMethodInfo
            .MakeGenericMethod(messageType)
            .Invoke(this, [filters])!;

        return (ILazyMessageStreamEnumerable<object>)lazyStream;
    }

    /// <inheritdoc cref="MessageStreamProvider.CreateLazyStream{TMessage}" />
    public override ILazyMessageStreamEnumerable<TMessageLinked> CreateLazyStream<TMessageLinked>(IReadOnlyCollection<IMessageFilter>? filters = null)
    {
        ILazyMessageStreamEnumerable<TMessageLinked> stream = new LazyMessageStreamEnumerable<TMessageLinked>(filters, OnSubscriberCompleted);

        lock (_lazyStreams)
        {
            _lazyStreams.Add((ILazyMessageStreamEnumerable)stream);
        }

        return stream;
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public override void Dispose()
    {
        if (_disposed)
            return;

        if (!_aborted && !_completed)
            CompleteAsync().SafeWait();

        _completeSemaphore.Wait();
        _completeSemaphore.Dispose();

        _disposed = true;
    }

    private static bool TryGetCompatibleMessage(
        ILazyMessageStreamEnumerable lazyStream,
        TMessage message,
        [NotNullWhen(true)] out object? compatibleMessage)
    {
        if (message == null || IsFiltered(lazyStream.Filters, message))
        {
            compatibleMessage = null;
            return false;
        }

        if (lazyStream.MessageType.IsInstanceOfType(message))
        {
            compatibleMessage = message;
            return true;
        }

        if (message is IEnvelope { Message: not null } envelope && lazyStream.MessageType.IsInstanceOfType(envelope.Message))
        {
            compatibleMessage = envelope.Message;
            return true;
        }

        compatibleMessage = null;
        return false;
    }

    private static bool IsFiltered(IReadOnlyCollection<IMessageFilter>? filters, object message) =>
        filters != null && filters.Count != 0 && !filters.All(filter => filter.MustProcess(message));

    // The subscriber of the stream returned without receiving the message and is invoked again with the stream replacing it (see
    // OnSubscriberCompleted): push the message to that stream instead. The message is refused only once, since it's the first one pushed
    // to the replacement, which acknowledges it if its subscriber returns again without receiving it.
    private static async Task PushToReplacementIfRefusedAsync(
        Task<bool> pushTask,
        ILazyMessageStreamEnumerable lazyStream,
        object message,
        Action<object?>? onPullAction,
        object? onPullActionArgument,
        CancellationToken cancellationToken)
    {
        while (!await pushTask.ConfigureAwait(false))
        {
            lazyStream = lazyStream.Replacement ?? throw new InvalidOperationException("The stream was detached without a replacement.");
            pushTask = lazyStream.GetOrCreateStream().PushAsync(message, onPullAction, onPullActionArgument, cancellationToken);
        }
    }

    private IEnumerable<Task> PushToCompatibleStreams(
        TMessage message,
        Action<object?>? onPullAction,
        object? onPullActionArgument,
        CancellationToken cancellationToken)
    {
        foreach (ILazyMessageStreamEnumerable lazyStream in GetLazyStreams())
        {
            // Softly abort if the cancellation token is signaled, don't throw to avoid unnecessary logging when triggered by the
            // application shutdown
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (TryGetCompatibleMessage(lazyStream, message, out object? compatibleMessage))
                yield return PushToStreamAsync(lazyStream, compatibleMessage, onPullAction, onPullActionArgument, cancellationToken);
        }
    }

    private Task PushToStreamAsync(
        ILazyMessageStreamEnumerable lazyStream,
        object message,
        Action<object?>? onPullAction,
        object? onPullActionArgument,
        CancellationToken cancellationToken)
    {
        Task<bool> pushTask = lazyStream.GetOrCreateStream().PushAsync(message, onPullAction, onPullActionArgument, cancellationToken);

        // Without restarts the streams are never detached, so the push is never refused
        return _restartsReturningSubscribers
            ? PushToReplacementIfRefusedAsync(pushTask, lazyStream, message, onPullAction, onPullActionArgument, cancellationToken)
            : pushTask;
    }

    // Copy the list, since a returning subscriber can replace its stream while the streams are being pushed (see OnSubscriberCompleted)
    private ILazyMessageStreamEnumerable[] GetLazyStreams()
    {
        lock (_lazyStreams)
        {
            return [.. _lazyStreams];
        }
    }

    private ILazyMessageStreamEnumerable[] CloseAndGetLazyStreams()
    {
        lock (_lazyStreams)
        {
            _isClosed = true;
            return [.. _lazyStreams];
        }
    }

    // Invoked by a lazy stream, whose stream is created, when its subscriber returns or throws. Returns the lazy stream replacing it if the
    // subscriber is invoked again, because it returned while the subscribers of the other streams are still enumerating them.
    private ILazyMessageStreamEnumerable? OnSubscriberCompleted(ILazyMessageStreamEnumerable lazyStream, bool hasThrown, bool isCanceled)
    {
        lock (_lazyStreams)
        {
            if (hasThrown)
            {
                _hasSubscriberThrown = true;
                _hasSubscriberFailed |= !isCanceled;

                // The failure aborts the streams (e.g., see PublisherConsumerBehavior): the pending pushes are left to the abort. The
                // cancellation (e.g., because the consumer is stopping) doesn't, while the other subscribers are still enumerating or
                // waiting for their stream, and the pending push would never complete: end the streams as when a subscriber returns
                // because of the cancellation, unless a subscriber failed (its abort applies the error policies). The cancellation of a
                // single subscriber aborts the streams like a failure.
                if (!_restartsReturningSubscribers || !isCanceled || _isClosed || _hasSubscriberFailed || _lazyStreams.Count == 1)
                    return null;
            }
            else if (_lazyStreams.Exists(otherLazyStream => otherLazyStream.Stream != null && !otherLazyStream.HasSubscriberReturned))
            {
                // Leave the pending push to the abort or completion in progress, or to the abort caused by the failed subscriber
                if (!_restartsReturningSubscribers || _isClosed || _hasSubscriberThrown)
                    return null;

                // A subscriber returning because the consumer is stopping ends the streams instead, as if all the subscribers returned, so
                // that the pending message is rolled back (see PublisherConsumerBehavior). Do the same if the stream can't be replaced,
                // not to leave the pending push waiting.
                if (!isCanceled && ReplaceLazyStream(lazyStream) is { } replacement)
                    return replacement;
            }
        }

        _subscribersReturnedTaskCompletionSource.TrySetResult();
        return null;
    }

    // Must be called holding the _lazyStreams lock. The replacement takes the position of the lazy stream, which no longer receives
    // the pushed messages: the pending push is acknowledged or refused when the stream is detached.
    private ILazyMessageStreamEnumerable? ReplaceLazyStream(ILazyMessageStreamEnumerable lazyStream)
    {
        int index = _lazyStreams.IndexOf(lazyStream);

        if (index < 0)
            return null;

        ILazyMessageStreamEnumerable replacement = lazyStream.CreateReplacement();
        _lazyStreams[index] = replacement;
        return replacement;
    }
}
