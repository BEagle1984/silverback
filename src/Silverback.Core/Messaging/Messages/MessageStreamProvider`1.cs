// Copyright (c) 2025 Sergio Aquilini
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

    private MethodInfo? _genericCreateStreamMethodInfo;

    private bool _completed;

    private bool _aborted;

    /// <inheritdoc cref="MessageStreamProvider.MessageType" />
    public override Type MessageType => typeof(TMessage);

    /// <inheritdoc cref="MessageStreamProvider.StreamsCount" />
    public override int StreamsCount => _lazyStreams.Count;

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

        List<Task> processingTasks = PushToCompatibleStreams(message, onPullAction, onPullActionArgument, cancellationToken).ToList();

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
        if (_completed)
            throw new InvalidOperationException("The streams are already completed.");

        if (_aborted)
            return;

        _lazyStreams.ParallelForEach(
            lazyStream =>
            {
                if (lazyStream.Stream != null)
                    lazyStream.Stream.Abort();
                else
                    lazyStream.Cancel();
            });

        _aborted = true;
    }

    /// <inheritdoc cref="MessageStreamProvider.CompleteAsync" />
    public override async ValueTask CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (_aborted)
            throw new InvalidOperationException("The stream are already aborted.");

        if (_completed)
            return;

        await _lazyStreams.ParallelForEachAsync(
            async lazyStream =>
            {
                if (lazyStream.Stream != null)
                    await lazyStream.Stream.CompleteAsync(cancellationToken).ConfigureAwait(false);
                else
                    lazyStream.Cancel();
            }).ConfigureAwait(false);

        _completed = true;
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
        ILazyMessageStreamEnumerable<TMessageLinked> stream = CreateLazyStreamCore<TMessageLinked>(filters);

        lock (_lazyStreams)
        {
            _lazyStreams.Add((ILazyMessageStreamEnumerable)stream);
        }

        return stream;
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public override void Dispose()
    {
        if (!_aborted && !_completed)
            CompleteAsync().SafeWait();
    }

    private static LazyMessageStreamEnumerable<TMessageLinked> CreateLazyStreamCore<TMessageLinked>(IReadOnlyCollection<IMessageFilter>? filters = null) =>
        new(filters);

    private static bool PushIfCompatibleType(
        ILazyMessageStreamEnumerable lazyStream,
        TMessage message,
        Action<object?>? onPullAction,
        object? onPullActionArgument,
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
            messageProcessingTask = lazyStream.GetOrCreateStream().PushAsync(message, onPullAction, onPullActionArgument, cancellationToken);
            return true;
        }

        if (message is IEnvelope { Message: not null } envelope &&
            lazyStream.MessageType.IsInstanceOfType(envelope.Message))
        {
            messageProcessingTask = lazyStream.GetOrCreateStream().PushAsync(envelope.Message, onPullAction, onPullActionArgument, cancellationToken);
            return true;
        }

        messageProcessingTask = Task.CompletedTask;
        return false;
    }

    private static bool IsFiltered(IReadOnlyCollection<IMessageFilter>? filters, object message) =>
        filters != null && filters.Count != 0 && !filters.All(filter => filter.MustProcess(message));

    private IEnumerable<Task> PushToCompatibleStreams(TMessage message, Action<object?>? onPullAction, object? onPullActionArgument, CancellationToken cancellationToken)
    {
        foreach (ILazyMessageStreamEnumerable? lazyStream in _lazyStreams)
        {
            // Softly abort if the cancellation token is signaled, don't throw to avoid unnecessary logging when triggered by the
            // application shutdown
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (PushIfCompatibleType(lazyStream, message, onPullAction, onPullActionArgument, cancellationToken, out Task processingTask))
            {
                yield return processingTask;
            }
        }
    }
}
