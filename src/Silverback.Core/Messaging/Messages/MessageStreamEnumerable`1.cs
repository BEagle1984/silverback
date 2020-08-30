// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc cref="IMessageStreamEnumerable{TMessage}" />
    public class MessageStreamEnumerable<TMessage> : IMessageStreamEnumerable<TMessage>, IDisposable, IPushableStream
    {
        private readonly Channel<TMessage> _channel;

        private ConcurrentDictionary<Type, IPushableStream>? _linkedStreams;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageStreamEnumerable{TMessage}" /> class.
        /// </summary>
        /// <param name="bufferCapacity">
        ///     The maximum number of messages that will be stored before blocking the <see cref="PushAsync" />
        ///     operations.
        /// </param>
        public MessageStreamEnumerable(int bufferCapacity = 1)
        {
            _channel = bufferCapacity > 0
                ? Channel.CreateBounded<TMessage>(bufferCapacity)
                : Channel.CreateUnbounded<TMessage>();
        }

        /// <summary>
        ///     Add the specified message to the stream.
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
        public async ValueTask PushAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            Check.NotNull<object>(message, nameof(message));

            await _channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);

            if (_linkedStreams == null)
                return;

            await _linkedStreams.ForEachAsync(pair => PushIfCompatibleType(pair.Value, pair.Key, message))
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     Marks the stream as complete, meaning no more messages will be pushed. This will also end the
        ///     enumerator loop.
        /// </summary>
        public void Complete()
        {
            _channel.Writer.Complete();

            _linkedStreams?.ForEach(pair => pair.Value.Complete());
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
        public IEnumerator<TMessage> GetEnumerator()
        {
            // TODO: Check this pattern!
            while (AsyncHelper.RunSynchronously(() => _channel.Reader.WaitToReadAsync().AsTask()))
            {
                if (_channel.Reader.TryRead(out var message))
                    yield return message!;
            }
        }

        /// <inheritdoc cref="IAsyncEnumerable{T}.GetAsyncEnumerator" />
        public IAsyncEnumerator<TMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            GetAsyncEnumerable(cancellationToken).GetAsyncEnumerator(cancellationToken);

        /// <summary>
        ///     Creates a new <see cref="IMessageStreamEnumerable{TMessage}" /> of a different message type that is
        ///     linked with this instance and will be pushed with the same messages.
        /// </summary>
        /// <typeparam name="TMessageLinked">
        ///     The type of the messages being streamed to the linked stream.
        /// </typeparam>
        /// <returns>
        ///     The linked <see cref="IMessageStreamEnumerable{TMessage}" />.
        /// </returns>
        public IMessageStreamEnumerable<TMessageLinked> GetLinkedStream<TMessageLinked>()
        {
            _linkedStreams ??= new ConcurrentDictionary<Type, IPushableStream>();
            return (IMessageStreamEnumerable<TMessageLinked>)_linkedStreams.GetOrAdd(
                typeof(TMessageLinked),
                _ => new MessageStreamEnumerable<TMessageLinked>(-1));
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc cref="IPushableStream.PushAsync" />
        ValueTask IPushableStream.PushAsync(object message, CancellationToken cancellationToken) =>
            PushAsync((TMessage)message, cancellationToken);

        /// <inheritdoc cref="IEnumerable.GetEnumerator" />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
                Complete();
        }

        private static async Task PushIfCompatibleType(IPushableStream stream, Type targetType, TMessage message)
        {
            if (message == null)
                return;

            if (targetType.IsInstanceOfType(message))
                await stream.PushAsync(message).ConfigureAwait(false);

            var envelope = message as IEnvelope;
            if (envelope?.Message != null && targetType.IsInstanceOfType(envelope.Message))
                await stream.PushAsync(envelope.Message).ConfigureAwait(false);
        }

        private async IAsyncEnumerable<TMessage> GetAsyncEnumerable(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_channel.Reader.TryRead(out var message))
                    yield return message!;
            }
        }
    }
}
