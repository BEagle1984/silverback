// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Messages;

internal abstract class MessageStreamProvider : IMessageStreamProvider, IDisposable
{
    /// <inheritdoc cref="IMessageStreamProvider.MessageType" />
    public abstract Type MessageType { get; }

    /// <inheritdoc cref="IMessageStreamProvider.StreamsCount" />
    public abstract int StreamsCount { get; }

    /// <summary>
    ///     Gets a <see cref="Task" /> that completes when the subscribers of all the streams created so far returned without throwing,
    ///     meaning that they stopped enumerating them. The subscribers whose stream wasn't created yet aren't awaited, including the ones
    ///     invoked again with a new stream after returning while the others were still enumerating. With these restarts enabled, it also
    ///     completes as soon as a subscriber returns or throws the cancellation because the cancellation token passed to it was canceled
    ///     (see <see cref="MessageStreamProvider{TMessage}(bool)" />).
    /// </summary>
    public abstract Task SubscribersReturnedTask { get; }

    /// <inheritdoc cref="IMessageStreamProvider.CreateStream" />
    public abstract IMessageStreamEnumerable<object> CreateStream(
        Type messageType,
        IReadOnlyCollection<IMessageFilter>? filters = null);

    /// <inheritdoc cref="IMessageStreamProvider.CreateStream{TMessage}" />
    public abstract IMessageStreamEnumerable<TMessage> CreateStream<TMessage>(IReadOnlyCollection<IMessageFilter>? filters = null);

    /// <inheritdoc cref="IMessageStreamProvider.CreateLazyStream" />
    public abstract ILazyMessageStreamEnumerable<object> CreateLazyStream(
        Type messageType,
        IReadOnlyCollection<IMessageFilter>? filters = null);

    /// <inheritdoc cref="IMessageStreamProvider.CreateLazyStream{TMessage}" />
    public abstract ILazyMessageStreamEnumerable<TMessage> CreateLazyStream<TMessage>(IReadOnlyCollection<IMessageFilter>? filters = null);

    /// <inheritdoc cref="Abort" />
    /// <remarks>
    ///     The abort is performed only if the streams haven't been completed already.
    /// </remarks>
    public abstract void AbortIfPending();

    /// <summary>
    ///     Aborts the ongoing enumerations and the pending calls to <see cref="MessageStreamProvider{TMessage}.PushAsync" />, then marks the
    ///     stream as complete. Calling this method will cause an <see cref="OperationCanceledException" /> to be thrown by the enumerators
    ///     and the <see cref="MessageStreamProvider{TMessage}.PushAsync" /> method.
    /// </summary>
    public abstract void Abort();

    /// <summary>
    ///     Marks the stream as complete, meaning no more messages will be pushed.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    public abstract ValueTask CompleteAsync(CancellationToken cancellationToken = default);

    public abstract void Dispose();
}
