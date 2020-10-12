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
    internal class MessageStreamProvider<TMessage> : IMessageStreamProviderInternal, IDisposable
    {
        private readonly List<IMessageStreamEnumerable> _streams = new List<IMessageStreamEnumerable>();

        private readonly ConcurrentDictionary<int, int> _streamsByMessage = new ConcurrentDictionary<int, int>();

        private int _messagesCount;

        private MethodInfo? _genericCreateStreamMethodInfo;

        // TODO: Checks if callbacks are used/needed

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
        ///     Gets or sets the callback function to be invoked when the stream is aborted, meaning no more messages
        ///     will be pushed but the stream is incomplete.
        /// </summary>
        public Func<Task>? PushAbortedCallback { get; set; }

        /// <summary>
        ///     Gets or sets the callback function to be invoked when the enumerable has been completed and all
        ///     messages have been pulled.
        /// </summary>
        public Func<Task>? EnumerationCompletedCallback { get; set; }

        /// <inheritdoc cref="IMessageStreamProvider.MessageType" />
        public Type MessageType => typeof(TMessage);

        /// <inheritdoc cref="IMessageStreamProvider.StreamsCount" />
        public int StreamsCount => _streams.Count;

        /// <summary>
        ///     Adds the specified message to the stream. The returned <see cref="Task" /> will complete only when the
        ///     message has actually been pulled and processed.
        /// </summary>
        /// <param name="message">
        ///     The message to be added.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The  <see cref="Task" /> will complete
        ///     only when the message has actually been pulled and processed.
        /// </returns>
        public virtual async Task PushAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            Check.NotNull<object>(message, nameof(message));

            var messageId = Interlocked.Increment(ref _messagesCount);

            // ReSharper disable once InconsistentlySynchronizedField
            var pushTasks = _streams.Select(
                stream => PushIfCompatibleTypeAsync(stream, messageId, message, cancellationToken));

            pushTasks = pushTasks.Where(task => task != null).ToArray();
            var pushedCount = ((Task?[])pushTasks).Length;

            if (pushedCount == 0 && ProcessedCallback != null)
            {
                await ProcessedCallback.Invoke(message).ConfigureAwait(false);
            }
            else
            {
                if (pushedCount > 1)
                {
                    if (!_streamsByMessage.TryAdd(messageId, pushedCount))
                    {
                        throw new InvalidOperationException("The same message was already pushed to this stream.");
                    }
                }

                await Task.WhenAll(pushTasks).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Aborts the ongoing enumerations and the pending calls to <see cref="PushAsync" />, then marks the
        ///     stream as complete. Calling this method will cause an <see cref="OperationCanceledException" /> to be
        ///     thrown by the enumerators and the <see cref="PushAsync" /> method.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        public async Task AbortAsync()
        {
            // ReSharper disable once InconsistentlySynchronizedField
            _streams.ParallelForEach(stream => stream.Abort());

            if (PushAbortedCallback != null)
                await PushAbortedCallback.Invoke().ConfigureAwait(false);
        }

        /// <summary>
        ///     Marks the stream as complete, meaning no more messages will be pushed.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            // ReSharper disable once InconsistentlySynchronizedField
            await _streams.ParallelForEach(stream => stream.CompleteAsync(cancellationToken))
                .ConfigureAwait(false);

            if (PushCompletedCallback != null)
                await PushCompletedCallback.Invoke().ConfigureAwait(false); // TODO: Should forward cancellation token?
        }

        /// <inheritdoc cref="IMessageStreamProvider.CreateStream" />
        public IMessageStreamEnumerable<object> CreateStream(Type messageType)
        {
            _genericCreateStreamMethodInfo ??= GetType().GetMethod(
                "CreateStream",
                1,
                Array.Empty<Type>());

            object stream = _genericCreateStreamMethodInfo
                .MakeGenericMethod(messageType)
                .Invoke(this, Array.Empty<object>());

            return (IMessageStreamEnumerable<object>)stream;
        }

        /// <inheritdoc cref="IMessageStreamProvider.CreateStream{TMessageLinked}" />
        public IMessageStreamEnumerable<TMessageLinked> CreateStream<TMessageLinked>()
        {
            var stream = CreateStreamCore<TMessageLinked>();

            lock (_streams)
            {
                _streams.Add((IMessageStreamEnumerable)stream);
            }

            return stream;
        }

        public Task NotifyStreamProcessedAsync(PushedMessage pushedMessage)
        {
            if (ProcessedCallback == null)
                return Task.CompletedTask;

            var messageId = pushedMessage.Id;

            if (_streamsByMessage.ContainsKey(messageId))
            {
                lock (_streamsByMessage)
                {
                    if (!_streamsByMessage.TryGetValue(messageId, out var count))
                        return Task.CompletedTask;

                    var isSuccess = count == 1
                        ? _streamsByMessage.TryRemove(messageId, out var _)
                        : _streamsByMessage.TryUpdate(messageId, count - 1, count);

                    if (!isSuccess)
                        throw new InvalidOperationException("The dictionary was modified.");

                    if (count > 1)
                        return Task.CompletedTask;
                }
            }

            return ProcessedCallback.Invoke((TMessage)pushedMessage.OriginalMessage);
        }

        public async Task NotifyStreamEnumerationCompletedAsync(IMessageStreamEnumerable stream)
        {
            int streamsCount;

            lock (_streams)
            {
                _streams.Remove(stream);

                streamsCount = _streams.Count;
            }

            if (streamsCount == 0 && EnumerationCompletedCallback != null)
                await EnumerationCompletedCallback.Invoke().ConfigureAwait(false);
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Creates a new <see cref="IMessageStreamEnumerable{TMessage}" /> of that will be linked with this
        ///     provider and will be pushed with the messages matching the type <typeparamref name="TMessageLinked" />.
        /// </summary>
        /// <typeparam name="TMessageLinked">
        ///     The type of the messages being streamed to the linked stream.
        /// </typeparam>
        /// <returns>
        ///     The new stream.
        /// </returns>
        protected virtual IMessageStreamEnumerable<TMessageLinked> CreateStreamCore<TMessageLinked>() =>
            new MessageStreamEnumerable<TMessageLinked>(this);

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
            // TODO: Prevent complete being called after abort (or being called twice)
            if (disposing)
                AsyncHelper.RunSynchronously(() => CompleteAsync());
        }

        private static Task? PushIfCompatibleTypeAsync(
            IMessageStreamEnumerable stream,
            int messageId,
            TMessage message,
            CancellationToken cancellationToken)
        {
            if (message == null)
                return null;

            if (stream.MessageType.IsInstanceOfType(message))
            {
                var pushedMessage = new PushedMessage(messageId, message, message);
                return stream.PushAsync(pushedMessage, cancellationToken);
            }

            var envelope = message as IEnvelope;
            if (envelope?.Message != null && envelope.AutoUnwrap &&
                stream.MessageType.IsInstanceOfType(envelope.Message))
            {
                var pushedMessage = new PushedMessage(messageId, envelope.Message, message);
                return stream.PushAsync(pushedMessage, cancellationToken);
            }

            return null;
        }
    }
}
