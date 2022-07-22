// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Messages
{
    internal abstract class MessageStreamProvider : IMessageStreamProvider, IDisposable
    {
        /// <inheritdoc cref="IMessageStreamProvider.MessageType" />
        public abstract Type MessageType { get; }

        /// <inheritdoc cref="IMessageStreamProvider.StreamsCount" />
        public abstract int StreamsCount { get; }

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

        /// <inheritdoc cref="Abort"/>
        /// <remarks>
        ///     The abort is performed only if the streams haven't been completed already.
        /// </remarks>
        public abstract void AbortIfPending();

        /// <summary>
        ///     Aborts the ongoing enumerations and the pending calls to
        ///     <see cref="MessageStreamProvider{TMessage}.PushAsync(TMessage,System.Threading.CancellationToken)" />, then marks the
        ///     stream as complete. Calling this method will cause an <see cref="OperationCanceledException" /> to be
        ///     thrown by the enumerators and the <see cref="MessageStreamProvider{TMessage}.PushAsync(TMessage,System.Threading.CancellationToken)" /> method.
        /// </summary>
        public abstract void Abort();

        /// <summary>
        ///     Marks the stream as complete, meaning no more messages will be pushed.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        public abstract Task CompleteAsync(CancellationToken cancellationToken = default);

        public abstract void Dispose();
    }
}
