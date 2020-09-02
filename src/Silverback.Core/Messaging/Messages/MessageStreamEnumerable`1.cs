// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc cref="IMessageStreamEnumerable{TMessage}" />
    internal class MessageStreamEnumerable<TMessage>
        : IMessageStreamEnumerable<TMessage>, IDisposable, IWritableMessageStream
    {
        private readonly object _initializationSyncLock = new object();

        private readonly int _bufferCapacity;

        private Channel<TMessage>? _channel;

        private List<IWritableMessageStream>? _linkedStreams;

        private int _enumeratorsCount;

        // ReSharper disable once RedundantDefaultMemberInitializer (problem with nullable)
        [AllowNull]
        private TMessage _current = default;

        private MethodInfo? _genericCreateLinkedStreamMethodInfo;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageStreamEnumerable{TMessage}" /> class.
        /// </summary>
        /// <param name="bufferCapacity">
        ///     The maximum number of messages that will be stored before blocking the <see cref="PushAsync" />
        ///     operations.
        /// </param>
        public MessageStreamEnumerable(int bufferCapacity = 1)
        {
            _bufferCapacity = bufferCapacity;
        }

        /// <summary>
        ///     Gets or sets the callback function to be invoked when the enumerable has been completed, meaning no
        ///     more messages will be pushed.
        /// </summary>
        public Func<Task>? PushCompletedCallback { get; set; }

        /// <summary>
        ///     Gets or sets the callback function to be invoked when a message is pulled and successfully processed.
        /// </summary>
        /// <remarks>
        ///     This callback is actually invoked when the next message is pulled.
        /// </remarks>
        public Func<TMessage, Task>? ProcessedCallback { get; set; }

        /// <summary>
        ///     Gets or sets the callback function to be invoked when the enumerable has been completed and all
        ///     messages have been pulled.
        /// </summary>
        public Func<Task>? EnumerationCompletedCallback { get; set; }

        /// <inheritdoc cref="IWritableMessageStream.MessageType" />
        Type IWritableMessageStream.MessageType => typeof(TMessage);

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

            if (_linkedStreams == null)
            {
                await EnsureChannelIsInitialized()
                    .Writer.WriteAsync(message, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await _linkedStreams.ParallelForEachAsync(
                        linkedStream => PushIfCompatibleType(linkedStream, message, cancellationToken))
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Marks the stream as complete, meaning no more messages will be pushed.
        /// </summary>
        public void Complete()
        {
            if (_linkedStreams == null)
                EnsureChannelIsInitialized().Writer.Complete();
            else
                _linkedStreams?.ParallelForEach(linkedStream => linkedStream.Complete());

            PushCompletedCallback?.Invoke();
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
        public IEnumerator<TMessage> GetEnumerator() =>
            EnumerateExclusively(() => GetEnumerable().GetEnumerator());

        /// <inheritdoc cref="IAsyncEnumerable{T}.GetAsyncEnumerator" />
        public IAsyncEnumerator<TMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            EnumerateExclusively(() => GetAsyncEnumerable(cancellationToken).GetAsyncEnumerator(cancellationToken));

        /// <inheritdoc cref="IWritableMessageStream.CreateLinkedStream" />
        public IMessageStreamEnumerable<object> CreateLinkedStream(Type messageType)
        {
            _genericCreateLinkedStreamMethodInfo ??= GetType().GetMethod(
                "CreateLinkedStream",
                1,
                Array.Empty<Type>());

            object linkedStream = _genericCreateLinkedStreamMethodInfo
                .MakeGenericMethod(messageType)
                .Invoke(this, Array.Empty<object>());

            return (IMessageStreamEnumerable<object>)linkedStream;
        }

        /// <inheritdoc cref="IWritableMessageStream.CreateLinkedStream{TMessageLinked}" />
        public IMessageStreamEnumerable<TMessageLinked> CreateLinkedStream<TMessageLinked>()
        {
            lock (_initializationSyncLock)
            {
                if (_channel != null)
                {
                    throw new InvalidOperationException(
                        "Cannot create a linked stream from a stream that " +
                        "is being enumerated or has completed already.");
                }

                var newStream = CreateLinkedStreamCore<TMessageLinked>(_bufferCapacity);

                _linkedStreams ??= new List<IWritableMessageStream>();
                _linkedStreams.Add((IWritableMessageStream)newStream);

                return newStream;
            }
        }

        /// <inheritdoc cref="IWritableMessageStream.PushAsync" />
        ValueTask IWritableMessageStream.PushAsync(object message, CancellationToken cancellationToken) =>
            PushAsync((TMessage)message, cancellationToken);

        /// <inheritdoc cref="IEnumerable.GetEnumerator" />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
        ///     The maximum number of messages that will be stored before blocking the <see cref="PushAsync" />
        ///     operations.
        /// </param>
        /// <typeparam name="TMessageLinked">
        ///     The type of the messages being streamed to the linked stream.
        /// </typeparam>
        /// <returns>
        ///     The new stream.
        /// </returns>
        protected virtual IMessageStreamEnumerable<TMessageLinked> CreateLinkedStreamCore<TMessageLinked>(
            int bufferCapacity) =>
            new MessageStreamEnumerable<TMessageLinked>(bufferCapacity);

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

        private static async Task PushIfCompatibleType(
            IWritableMessageStream stream,
            TMessage message,
            CancellationToken cancellationToken)
        {
            if (message == null)
                return;

            if (stream.MessageType.IsInstanceOfType(message))
                await stream.PushAsync(message, cancellationToken).ConfigureAwait(false);

            var envelope = message as IEnvelope;
            if (envelope?.Message != null && stream.MessageType.IsInstanceOfType(envelope.Message))
                await stream.PushAsync(envelope.Message, cancellationToken).ConfigureAwait(false);
        }

        private Channel<TMessage> EnsureChannelIsInitialized()
        {
            lock (_initializationSyncLock)
            {
                if (_linkedStreams != null)
                {
                    throw new InvalidOperationException(
                        "A channel cannot be created in this stream " +
                        "because it has linked streams. " +
                        "This stream cannot be enumerated.");
                }

                return _channel ??= _bufferCapacity > 0
                    ? Channel.CreateBounded<TMessage>(_bufferCapacity)
                    : Channel.CreateUnbounded<TMessage>();
            }
        }

        private IEnumerable<TMessage> GetEnumerable()
        {
            // TODO: Check this pattern!
            while (AsyncHelper.RunSynchronously(() => TryReadAsync(CancellationToken.None)))
            {
                yield return _current;
                ProcessedCallback?.Invoke(_current);
            }

            EnumerationCompletedCallback?.Invoke();
        }

        private async IAsyncEnumerable<TMessage> GetAsyncEnumerable(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (await TryReadAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return _current!;
                ProcessedCallback?.Invoke(_current);
            }

            EnumerationCompletedCallback?.Invoke();
        }

        private TReturn EnumerateExclusively<TReturn>(Func<TReturn> action)
        {
            if (_enumeratorsCount > 0)
                throw new InvalidOperationException("Only one concurrent enumeration is allowed.");

            Interlocked.Increment(ref _enumeratorsCount);

            if (_enumeratorsCount > 1)
                throw new InvalidOperationException("Only one concurrent enumeration is allowed.");

            return action.Invoke();
        }

        private async Task<bool> TryReadAsync(CancellationToken cancellationToken)
        {
            var channel = EnsureChannelIsInitialized();

            CancellationTokenSource? linkedTokenSource = null;

            try
            {
                await channel.Reader.WaitToReadAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            finally
            {
                linkedTokenSource?.Dispose();
            }

            return channel.Reader.TryRead(out _current);
        }
    }
}
