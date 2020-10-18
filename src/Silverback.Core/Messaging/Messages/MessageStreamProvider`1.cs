// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Relays the streamed messages to all the linked <see cref="MessageStreamEnumerable{TMessage}" />.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages being streamed.
    /// </typeparam>
    internal class MessageStreamProvider<TMessage> : IMessageStreamProvider, IDisposable
    {
        private readonly List<IMessageStreamEnumerable> _streams = new List<IMessageStreamEnumerable>();

        private int _messagesCount;

        private MethodInfo? _genericCreateStreamMethodInfo;

        /// <inheritdoc cref="IMessageStreamProvider.MessageType" />
        public Type MessageType => typeof(TMessage);

        /// <inheritdoc cref="IMessageStreamProvider.AllowSubscribeAsEnumerable" />
        public bool AllowSubscribeAsEnumerable { get; set; } = true;

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
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task will complete only
        ///     when the message has actually been pulled and processed and its result contains the number of
        ///     <see cref="IMessageStreamEnumerable{TMessage}" /> that have been pushed.
        /// </returns>
        public virtual Task<int> PushAsync(TMessage message, CancellationToken cancellationToken = default) =>
            PushAsync(message, true, cancellationToken);

        /// <summary>
        ///     Adds the specified message to the stream. The returned <see cref="Task" /> will complete only when the
        ///     message has actually been pulled and processed.
        /// </summary>
        /// <param name="message">
        ///     The message to be added.
        /// </param>
        /// <param name="throwIfUnhandled">
        ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the
        ///     message.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task will complete only
        ///     when the message has actually been pulled and processed and its result contains the number of
        ///     <see cref="IMessageStreamEnumerable{TMessage}" /> that have been pushed.
        /// </returns>
        public virtual async Task<int> PushAsync(TMessage message, bool throwIfUnhandled, CancellationToken cancellationToken = default)
        {
            Check.NotNull<object>(message, nameof(message));

            var messageId = Interlocked.Increment(ref _messagesCount);

            // ReSharper disable once InconsistentlySynchronizedField
            var pushTasks = _streams
                .Select(stream => PushIfCompatibleTypeAsync(stream, messageId, message, cancellationToken))
                .Where(task => task != null)
                .ToArray();

            if (pushTasks.Length > 0)
                await Task.WhenAll(pushTasks).ConfigureAwait(false);

            if (pushTasks.Length == 0 && throwIfUnhandled)
                throw new UnhandledMessageException(message!);

            return pushTasks.Length;
        }

        /// <summary>
        ///     Aborts the ongoing enumerations and the pending calls to <see cref="PushAsync" />, then marks the
        ///     stream as complete. Calling this method will cause an <see cref="OperationCanceledException" /> to be
        ///     thrown by the enumerators and the <see cref="PushAsync" /> method.
        /// </summary>
        public void Abort()
        {
            _streams.ParallelForEach(stream => stream.Abort());
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
            await _streams.ParallelForEachAsync(stream => stream.CompleteAsync(cancellationToken))
                .ConfigureAwait(false);
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

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private static IMessageStreamEnumerable<TMessageLinked> CreateStreamCore<TMessageLinked>() =>
            new MessageStreamEnumerable<TMessageLinked>();

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

        private void Dispose(bool disposing)
        {
            // TODO: Prevent complete being called after abort (or being called twice)
            if (disposing)
                AsyncHelper.RunSynchronously(() => CompleteAsync());
        }
    }
}
