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

    private readonly bool _detachesReturningSubscribers;

    private MethodInfo? _genericCreateStreamMethodInfo;

    private bool _completed;

    private bool _aborted;

    private bool _disposed;

    private bool _isCompleting;

    private bool _isAborting;

    // Set holding the _lazyStreams lock when the streams are being aborted or completed: no stream is detached afterwards
    private bool _isClosed;

    // Set holding the _lazyStreams lock when a subscriber throws: no stream is detached afterwards
    private bool _hasSubscriberThrown;

    // Set holding the _lazyStreams lock when a subscriber throws an exception other than the cancellation (see OnSubscriberCompleted)
    private bool _hasSubscriberFailed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageStreamProvider{TMessage}" /> class.
    /// </summary>
    /// <param name="detachesReturningSubscribers">
    ///     A value indicating whether the stream of a subscriber that returns (e.g., with a <c>break</c>), while the subscribers of the other
    ///     streams are still enumerating them, is detached: the subscriber doesn't receive any further message, while the others keep
    ///     receiving them. It isn't if the cancellation token passed to the subscriber was canceled (e.g., because the consumer is stopping),
    ///     once a subscriber threw, or once the streams are being aborted or completed. A subscriber returning or throwing the cancellation
    ///     because the token was canceled instead completes the <see cref="SubscribersReturnedTask" /> as if all the subscribers returned,
    ///     unless the streams are already being aborted or completed or another subscriber failed (or, when throwing, it's the only
    ///     subscriber). Otherwise, its pending push is left to the completion or the abort, which are expected once all the subscribers
    ///     returned (see <see cref="SubscribersReturnedTask" />).
    /// </param>
    public MessageStreamProvider(bool detachesReturningSubscribers = false)
    {
        _detachesReturningSubscribers = detachesReturningSubscribers;
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

        List<Task> processingTasks = PushToCompatibleStreams(message, onPullAction, onPullActionArgument, cancellationToken);

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

            Close();
            _lazyStreams.ForEach(lazyStream => lazyStream.Abort());

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

            Close();
            await _lazyStreams
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

    private static bool PushIfCompatibleType(
        ILazyMessageStreamEnumerable lazyStream,
        TMessage message,
        Action<object?>? onPullAction,
        object? onPullActionArgument,
        ref List<ILazyMessageStreamEnumerable>? createdLazyStreams,
        CancellationToken cancellationToken,
        out Task messageProcessingTask)
    {
        if (message == null || IsFiltered(lazyStream.Filters, message))
        {
            messageProcessingTask = Task.CompletedTask;
            return false;
        }

        if (lazyStream.MessageType.IsInstanceOfType(message))
        {
            messageProcessingTask = GetOrCreateStream(lazyStream, ref createdLazyStreams)
                .PushAsync(message, onPullAction, onPullActionArgument, cancellationToken);
            return true;
        }

        if (message is IEnvelope { Message: not null } envelope && lazyStream.MessageType.IsInstanceOfType(envelope.Message))
        {
            messageProcessingTask = GetOrCreateStream(lazyStream, ref createdLazyStreams)
                .PushAsync(envelope.Message, onPullAction, onPullActionArgument, cancellationToken);
            return true;
        }

        messageProcessingTask = Task.CompletedTask;
        return false;
    }

    // The creation of the new streams is signaled, invoking their subscribers, once all the streams the message is pushed to are created
    // (see PushToCompatibleStreams)
    private static IMessageStreamEnumerable GetOrCreateStream(
        ILazyMessageStreamEnumerable lazyStream,
        ref List<ILazyMessageStreamEnumerable>? createdLazyStreams)
    {
        IMessageStreamEnumerable stream = lazyStream.GetOrCreateStreamWithoutSignaling(out bool isCreated);

        if (isCreated)
            (createdLazyStreams ??= []).Add(lazyStream);

        return stream;
    }

    private static bool IsFiltered(IReadOnlyCollection<IMessageFilter>? filters, object message) =>
        filters != null && filters.Count != 0 && !filters.All(filter => filter.MustProcess(message));

    private List<Task> PushToCompatibleStreams(
        TMessage message,
        Action<object?>? onPullAction,
        object? onPullActionArgument,
        CancellationToken cancellationToken)
    {
        List<Task> processingTasks = [];
        List<ILazyMessageStreamEnumerable>? createdLazyStreams = null;

        try
        {
            foreach (ILazyMessageStreamEnumerable lazyStream in _lazyStreams)
            {
                // Softly abort if the cancellation token is signaled, don't throw to avoid unnecessary logging when triggered by the
                // application shutdown
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (PushIfCompatibleType(
                    lazyStream,
                    message,
                    onPullAction,
                    onPullActionArgument,
                    ref createdLazyStreams,
                    cancellationToken,
                    out Task processingTask))
                {
                    processingTasks.Add(processingTask);
                }
            }
        }
        finally
        {
            // Invoke the subscribers of the new streams only once all the streams the message is pushed to are created, also if the push
            // fails midway: a subscriber returning right away then finds the streams of the other subscribers of the same message, which
            // are therefore still enumerating it (see OnSubscriberCompleted)
            if (createdLazyStreams != null)
            {
                foreach (ILazyMessageStreamEnumerable createdLazyStream in createdLazyStreams)
                {
                    createdLazyStream.SignalCreated();
                }
            }
        }

        return processingTasks;
    }

    // No stream is detached once the streams are being aborted or completed: the pending pushes are left to the abort or the completion
    private void Close()
    {
        lock (_lazyStreams)
        {
            _isClosed = true;
        }
    }

    // Invoked by a lazy stream, whose stream is created, when its subscriber returns or throws. Returns whether the stream must be
    // detached, because its subscriber returned while the subscribers of the other streams are still enumerating them.
    private bool OnSubscriberCompleted(bool hasThrown, bool isCanceled)
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
                if (!_detachesReturningSubscribers || !isCanceled || _isClosed || _hasSubscriberFailed || _lazyStreams.Count == 1)
                    return false;
            }
            else if (_lazyStreams.Exists(lazyStream => lazyStream.Stream != null && !lazyStream.HasSubscriberReturned))
            {
                // Leave the pending push to the abort or completion in progress, or to the abort caused by the failed subscriber
                if (!_detachesReturningSubscribers || _isClosed || _hasSubscriberThrown)
                    return false;

                // A subscriber returning because the consumer is stopping ends the streams instead, as if all the subscribers returned, so
                // that the pending message is rolled back (see PublisherConsumerBehavior)
                if (!isCanceled)
                    return true;
            }
        }

        _subscribersReturnedTaskCompletionSource.TrySetResult();
        return false;
    }
}
