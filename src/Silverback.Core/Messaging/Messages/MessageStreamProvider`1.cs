// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Relays the streamed messages to all the linked <see cref="MessageStreamEnumerable{TMessage}"/>.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages being streamed.
    /// </typeparam>
    internal class MessageStreamProvider<TMessage> : IMessageStreamProvider, IDisposable
    {
        private readonly int _bufferCapacity;

        private readonly List<IMessageStreamEnumerable> _linkedStreams =
            new List<IMessageStreamEnumerable>();

        private readonly ConcurrentDictionary<int, int> _linkedStreamsByMessage = new ConcurrentDictionary<int, int>();

        private int _messagesCount;

        private MethodInfo? _genericCreateLinkedStreamMethodInfo;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageStreamProvider{TMessage}" /> class.
        /// </summary>
        /// <param name="bufferCapacity">
        ///     The maximum number of messages that will be stored before blocking the <c>PushAsync</c>
        ///     operations.
        /// </param>
        public MessageStreamProvider(int bufferCapacity = 1)
        {
            _bufferCapacity = bufferCapacity;
        }

        /// <summary>
        ///     Gets or sets the callback function to be invoked when a message is pulled and successfully processed.
        /// </summary>
        /// <remarks>
        ///     This callback is actually invoked when the next message is pulled.
        /// </remarks>
        public Func<TMessage, Task>? ProcessedCallback { get; set; }

        /// <summary>
        ///     Gets or sets the callback function to be invoked when the enumerable has been completed, meaning no
        ///     more messages will be pushed.
        /// </summary>
        public Func<Task>? PushCompletedCallback { get; set; }

        /// <summary>
        ///     Gets or sets the callback function to be invoked when the enumerable has been completed and all
        ///     messages have been pulled.
        /// </summary>
        public Func<Task>? EnumerationCompletedCallback { get; set; }

        /// <inheritdoc cref="IMessageStreamProvider.MessageType" />
        public Type MessageType => typeof(TMessage);

        /// <summary>
        ///     Add the specified message to the stream. This overload is used by the owner stream to push to the
        ///     linked streams.
        /// </summary>
        /// <param name="message">
        ///     The message to be added.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the write operation.
        /// </param>
        /// <returns>
        ///     A <see cref="ValueTask" /> representing the asynchronous operation.
        /// </returns>
        public virtual async Task PushAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            Check.NotNull<object>(message, nameof(message));

            var messageId = Interlocked.Increment(ref _messagesCount);

            // ReSharper disable once InconsistentlySynchronizedField
            var results = await _linkedStreams.ParallelSelectAsync(
                    linkedStream => PushIfCompatibleType(linkedStream, messageId, message, cancellationToken))
                .ConfigureAwait(false);

            var count = results.Count(result => result);

            if (count == 0 && ProcessedCallback != null)
            {
                await ProcessedCallback.Invoke(message).ConfigureAwait(false);
            }
            else if (count > 1)
            {
                if (!_linkedStreamsByMessage.TryAdd(messageId, count))
                    throw new InvalidOperationException("The same message was already pushed to this stream.");
            }
        }

        /// <summary>
        ///     Marks the stream as complete, meaning no more messages will be pushed.
        /// </summary>
        public async Task CompleteAsync()
        {
            // ReSharper disable once InconsistentlySynchronizedField
            _linkedStreams.ParallelForEach(stream => stream.Complete());

            if (PushCompletedCallback != null)
                await PushCompletedCallback.Invoke().ConfigureAwait(false);
        }

        /// <inheritdoc cref="IMessageStreamProvider.CreateStream" />
        public IMessageStreamEnumerable<object> CreateStream(Type messageType)
        {
            _genericCreateLinkedStreamMethodInfo ??= GetType().GetMethod(
                "CreateStream",
                1,
                Array.Empty<Type>());

            object linkedStream = _genericCreateLinkedStreamMethodInfo
                .MakeGenericMethod(messageType)
                .Invoke(this, Array.Empty<object>());

            return (IMessageStreamEnumerable<object>)linkedStream;
        }

        /// <inheritdoc cref="IMessageStreamProvider.CreateStream{TMessageLinked}" />
        public IMessageStreamEnumerable<TMessageLinked> CreateStream<TMessageLinked>()
        {
            var stream = CreateStreamCore<TMessageLinked>(_bufferCapacity);

            lock (_linkedStreams)
            {
                _linkedStreams.Add((IMessageStreamEnumerable)stream);
            }

            return stream;
        }

        public Task NotifyLinkedStreamProcessed(PushedMessage pushedMessage)
        {
            if (ProcessedCallback == null)
                return Task.CompletedTask;

            var messageId = pushedMessage.Id;

            if (_linkedStreamsByMessage.ContainsKey(messageId))
            {
                lock (_linkedStreamsByMessage)
                {
                    if (!_linkedStreamsByMessage.TryGetValue(messageId, out var count))
                        return Task.CompletedTask;

                    var isSuccess = count == 1
                        ? _linkedStreamsByMessage.TryRemove(messageId, out var _)
                        : _linkedStreamsByMessage.TryUpdate(messageId, count - 1, count);

                    if (!isSuccess)
                        throw new InvalidOperationException("The dictionary was modified.");

                    if (count > 1)
                        return Task.CompletedTask;
                }
            }

            return ProcessedCallback.Invoke((TMessage)pushedMessage.OriginalMessage);
        }

        public async Task NotifyLinkedStreamEnumerationCompleted(IMessageStreamEnumerable linkedStream)
        {
            int linkedStreamsCount;

            lock (_linkedStreams)
            {
                _linkedStreams.Remove(linkedStream);

                linkedStreamsCount = _linkedStreams.Count;
            }

            if (linkedStreamsCount == 0 && EnumerationCompletedCallback != null)
                await EnumerationCompletedCallback.Invoke().ConfigureAwait(false);
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Creates a new <see cref="IMessageStreamEnumerable{TMessage}" /> of a different message type that is
        ///     linked with this instance and will be pushed with the same messages.
        /// </summary>
        /// <param name="bufferCapacity">
        ///     The maximum number of messages that will be stored before blocking the <c>PushAsync</c>
        ///     operations.
        /// </param>
        /// <typeparam name="TMessageLinked">
        ///     The type of the messages being streamed to the linked stream.
        /// </typeparam>
        /// <returns>
        ///     The new stream.
        /// </returns>
        protected virtual IMessageStreamEnumerable<TMessageLinked> CreateStreamCore<TMessageLinked>(
            int bufferCapacity) =>
            new MessageStreamEnumerable<TMessageLinked>(this, bufferCapacity);

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        ///     resources.
        /// </summary>
        /// <param name="disposing">
        ///     A value indicating whether the method has been called by the <c>Dispose</c> method and not from the
        ///     finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                AsyncHelper.RunSynchronously(CompleteAsync);
        }

        private static async Task<bool> PushIfCompatibleType(
            IMessageStreamEnumerable stream,
            int messageId,
            TMessage message,
            CancellationToken cancellationToken)
        {
            if (message == null)
                return false;

            if (stream.MessageType.IsInstanceOfType(message))
            {
                var pushedMessage = new PushedMessage(messageId, message, message);
                await stream.PushAsync(pushedMessage, cancellationToken).ConfigureAwait(false);
                return true;
            }

            var envelope = message as IEnvelope;
            if (envelope?.Message != null && stream.MessageType.IsInstanceOfType(envelope.Message))
            {
                var pushedMessage = new PushedMessage(messageId, envelope.Message, message);
                await stream.PushAsync(pushedMessage, cancellationToken).ConfigureAwait(false);
                return true;
            }

            return false;
        }
    }
}
