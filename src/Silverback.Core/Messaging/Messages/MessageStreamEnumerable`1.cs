// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        : IMessageStreamEnumerable<TMessage>, IMessageStreamEnumerable, IDisposable
    {
        private readonly object _initializationSyncLock = new object();

        private readonly int _bufferCapacity;

        private readonly IMessageStreamEnumerable? _ownerStream;

        private Channel<PushedMessage>? _channel;

        private List<IMessageStreamEnumerable>? _linkedStreams;

        private ConcurrentDictionary<int, int>? _linkedStreamsByMessage;

        private int _enumeratorsCount;

        private int _messagesCount;

        // ReSharper disable once RedundantDefaultMemberInitializer (problem with nullable)
        [AllowNull]
        private PushedMessage _current = default;

        private MethodInfo? _genericCreateLinkedStreamMethodInfo;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageStreamEnumerable{TMessage}" /> class.
        /// </summary>
        /// <param name="bufferCapacity">
        ///     The maximum number of messages that will be stored before blocking the <c>PushAsync</c>
        ///     operations.
        /// </param>
        public MessageStreamEnumerable(int bufferCapacity = 1)
        {
            _bufferCapacity = bufferCapacity;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageStreamEnumerable{TMessage}" /> class.
        /// </summary>
        /// <param name="ownerStream">
        ///     The owner of the linked stream.
        /// </param>
        /// <param name="bufferCapacity">
        ///     The maximum number of messages that will be stored before blocking the <c>PushAsync</c>
        ///     operations.
        /// </param>
        protected MessageStreamEnumerable(IMessageStreamEnumerable ownerStream, int bufferCapacity = 1)
            : this(bufferCapacity)
        {
            _ownerStream = ownerStream;
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

        /// <inheritdoc cref="IMessageStreamEnumerable.MessageType" />
        public Type MessageType => typeof(TMessage);

        /// <inheritdoc cref="IMessageStreamEnumerable.PushAsync(object,System.Threading.CancellationToken)" />
        public async Task PushAsync(object message, CancellationToken cancellationToken = default)
        {
            Check.NotNull(message, nameof(message));

            if (!(message is TMessage typedMessage))
            {
                throw new ArgumentException(
                    $"Expected a message of type {typeof(TMessage).FullName} but received a message of type {message.GetType().FullName}.",
                    nameof(message));
            }

            var messageId = Interlocked.Increment(ref _messagesCount);

            if (_linkedStreams == null)
            {
                var pushedMessage = new PushedMessage(messageId, message, message);
                await EnsureChannelIsInitialized()
                    .Writer.WriteAsync(pushedMessage, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                // ReSharper disable once InconsistentlySynchronizedField
                var results = await _linkedStreams.ParallelSelectAsync(
                        linkedStream => PushIfCompatibleType(linkedStream, messageId, typedMessage, cancellationToken))
                    .ConfigureAwait(false);

                var count = results.Count(result => result);

                if (count == 0 && ProcessedCallback != null)
                {
                    await ProcessedCallback.Invoke(typedMessage).ConfigureAwait(false);
                }
                else if (count > 1)
                {
                    // ReSharper disable once InconsistentlySynchronizedField
                    if (!_linkedStreamsByMessage!.TryAdd(messageId, count))
                        throw new InvalidOperationException("The same message was already pushed to this stream.");
                }
            }
        }

        /// <inheritdoc cref="IMessageStreamEnumerable.PushAsync(PushedMessage,System.Threading.CancellationToken)" />
        public async Task PushAsync(PushedMessage pushedMessage, CancellationToken cancellationToken = default)
        {
            Check.NotNull(pushedMessage, nameof(pushedMessage));

            if (_linkedStreams != null || _ownerStream == null)
                throw new InvalidOperationException("Cannot relay a PushedMessage to a stream that is not linked.");

            await EnsureChannelIsInitialized()
                .Writer.WriteAsync(pushedMessage, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     Marks the stream as complete, meaning no more messages will be pushed.
        /// </summary>
        public async Task CompleteAsync()
        {
            if (_linkedStreams == null)
            {
                EnsureChannelIsInitialized().Writer.Complete();
            }
            else
            {
                // ReSharper disable once InconsistentlySynchronizedField
                await _linkedStreams.ParallelForEachAsync(stream => stream.CompleteAsync()).ConfigureAwait(false);
            }

            if (PushCompletedCallback != null)
                await PushCompletedCallback.Invoke().ConfigureAwait(false);
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
        public IEnumerator<TMessage> GetEnumerator() =>
            EnumerateExclusively(() => GetEnumerable().GetEnumerator());

        /// <inheritdoc cref="IAsyncEnumerable{T}.GetAsyncEnumerator" />
        public IAsyncEnumerator<TMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            EnumerateExclusively(() => GetAsyncEnumerable(cancellationToken).GetAsyncEnumerator(cancellationToken));

        /// <inheritdoc cref="IMessageStreamEnumerable.CreateLinkedStream" />
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

        /// <inheritdoc cref="IMessageStreamEnumerable.CreateLinkedStream{TMessageLinked}" />
        public IMessageStreamEnumerable<TMessageLinked> CreateLinkedStream<TMessageLinked>()
        {
            if (_ownerStream != null)
                throw new InvalidOperationException("Cannot create a linked stream from a linked stream.");

            lock (_initializationSyncLock)
            {
                if (_channel != null)
                {
                    throw new InvalidOperationException(
                        "Cannot create a linked stream from a stream that " +
                        "is being enumerated or has completed already.");
                }

                var newStream = CreateLinkedStreamCore<TMessageLinked>(_bufferCapacity);

                _linkedStreams ??= new List<IMessageStreamEnumerable>();
                _linkedStreams.Add((IMessageStreamEnumerable)newStream);

                _linkedStreamsByMessage ??= new ConcurrentDictionary<int, int>();

                return newStream;
            }
        }

        public Task NotifyLinkedStreamProcessed(PushedMessage pushedMessage)
        {
            if (ProcessedCallback == null)
                return Task.CompletedTask;

            var messageId = pushedMessage.Id;

            // ReSharper disable once InconsistentlySynchronizedField
            if (_linkedStreamsByMessage != null && _linkedStreamsByMessage.ContainsKey(messageId))
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

            lock (_linkedStreams!)
            {
                _linkedStreams.Remove(linkedStream);

                linkedStreamsCount = _linkedStreams.Count;
            }

            if (linkedStreamsCount == 0 && EnumerationCompletedCallback != null)
                await EnumerationCompletedCallback.Invoke().ConfigureAwait(false);
        }

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
        ///     The maximum number of messages that will be stored before blocking the <c>PushAsync</c>
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

        private Channel<PushedMessage> EnsureChannelIsInitialized()
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
                    ? Channel.CreateBounded<PushedMessage>(_bufferCapacity)
                    : Channel.CreateUnbounded<PushedMessage>();
            }
        }

        private IEnumerable<TMessage> GetEnumerable()
        {
            // TODO: Check this pattern!
            while (AsyncHelper.RunSynchronously(() => TryReadAsync(CancellationToken.None)))
            {
                var currentMessage = (TMessage)_current.Message;
                yield return currentMessage;

                if (ProcessedCallback != null)
                    AsyncHelper.RunSynchronously(() => ProcessedCallback.Invoke(currentMessage));

                _ownerStream?.NotifyLinkedStreamProcessed(_current);
            }

            if (EnumerationCompletedCallback != null)
                AsyncHelper.RunSynchronously(() => EnumerationCompletedCallback.Invoke());
        }

        private async IAsyncEnumerable<TMessage> GetAsyncEnumerable(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (await TryReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var currentMessage = (TMessage)_current.Message;
                yield return currentMessage;

                if (ProcessedCallback != null)
                    await ProcessedCallback.Invoke(currentMessage).ConfigureAwait(false);

                _ownerStream?.NotifyLinkedStreamProcessed(_current);
            }

            if (EnumerationCompletedCallback != null)
                await EnumerationCompletedCallback.Invoke().ConfigureAwait(false);

            _ownerStream?.NotifyLinkedStreamEnumerationCompleted(this);
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
