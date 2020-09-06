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
        private readonly IMessageStreamProvider _ownerStreamProvider;

        private readonly Channel<PushedMessage> _channel;

        private int _enumeratorsCount;

        private PushedMessage? _current;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageStreamEnumerable{TMessage}" /> class.
        /// </summary>
        /// <param name="ownerStreamProvider">
        ///     The owner of the linked stream.
        /// </param>
        /// <param name="bufferCapacity">
        ///     The maximum number of messages that will be stored before blocking the <c>PushAsync</c>
        ///     operations.
        /// </param>
        public MessageStreamEnumerable(IMessageStreamProvider ownerStreamProvider, int bufferCapacity = 1)
        {
            _ownerStreamProvider = ownerStreamProvider;

            _channel = bufferCapacity > 0
                ? Channel.CreateBounded<PushedMessage>(bufferCapacity)
                : Channel.CreateUnbounded<PushedMessage>();
        }

        /// <inheritdoc cref="IMessageStreamEnumerable.MessageType" />
        public Type MessageType => typeof(TMessage);

        /// <inheritdoc cref="IMessageStreamEnumerable.PushAsync(PushedMessage,System.Threading.CancellationToken)" />
        public async Task PushAsync(PushedMessage pushedMessage, CancellationToken cancellationToken = default)
        {
            Check.NotNull(pushedMessage, nameof(pushedMessage));

            await _channel
                .Writer.WriteAsync(pushedMessage, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc cref="IMessageStreamEnumerable.Complete" />
        public void Complete() => _channel.Writer.Complete();

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
        public IEnumerator<TMessage> GetEnumerator() =>
            EnumerateExclusively(() => GetEnumerable().GetEnumerator());

        /// <inheritdoc cref="IAsyncEnumerable{T}.GetAsyncEnumerator" />
        public IAsyncEnumerator<TMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            EnumerateExclusively(() => GetAsyncEnumerable(cancellationToken).GetAsyncEnumerator(cancellationToken));

        /// <inheritdoc cref="IEnumerable.GetEnumerator" />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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

        private IEnumerable<TMessage> GetEnumerable()
        {
            // TODO: Check this pattern!
            while (AsyncHelper.RunSynchronously(() => TryReadAsync(CancellationToken.None)))
            {
                if (_current == null)
                    continue;

                var currentMessage = (TMessage)_current.Message;
                yield return currentMessage;

                AsyncHelper.RunSynchronously(() => _ownerStreamProvider.NotifyLinkedStreamProcessed(_current));
            }

            AsyncHelper.RunSynchronously(() => _ownerStreamProvider.NotifyLinkedStreamEnumerationCompleted(this));
        }

        private async IAsyncEnumerable<TMessage> GetAsyncEnumerable(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (await TryReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_current == null)
                    continue;

                var currentMessage = (TMessage)_current.Message;
                yield return currentMessage;

                await _ownerStreamProvider.NotifyLinkedStreamProcessed(_current).ConfigureAwait(false);
            }

            await _ownerStreamProvider.NotifyLinkedStreamEnumerationCompleted(this).ConfigureAwait(false);
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
            CancellationTokenSource? linkedTokenSource = null;

            try
            {
                await _channel.Reader.WaitToReadAsync(cancellationToken)
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

            return _channel.Reader.TryRead(out _current);
        }
    }
}
