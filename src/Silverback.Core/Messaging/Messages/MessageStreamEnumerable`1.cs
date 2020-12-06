// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc cref="IMessageStreamEnumerable{TMessage}" />
    /// <remarks>
    ///     This implementation is not thread-safe.
    /// </remarks>
    internal class MessageStreamEnumerable<TMessage>
        : IMessageStreamEnumerable<TMessage>, IMessageStreamEnumerable, IDisposable
    {
        private readonly SemaphoreSlim _writeSemaphore = new(1, 1);

        private readonly SemaphoreSlim _readSemaphore = new(0, 1);

        private readonly SemaphoreSlim _processedSemaphore = new(0, 1);

        private readonly CancellationTokenSource _abortCancellationTokenSource = new();

        private readonly object _completeLock = new();

        private PushedMessage? _current;

        private bool _isFirstMessage = true;

        private bool _isComplete;

        /// <inheritdoc cref="IMessageStreamEnumerable.PushAsync(PushedMessage,System.Threading.CancellationToken)" />
        [SuppressMessage(
            "ReSharper",
            "InconsistentlySynchronizedField",
            Justification = "The lock is important to avoid multiple complete/abort, here is not important")]
        public async Task PushAsync(PushedMessage pushedMessage, CancellationToken cancellationToken = default)
        {
            Check.NotNull(pushedMessage, nameof(pushedMessage));

            using var linkedTokenSource = LinkWithAbortCancellationTokenSource(cancellationToken);

            await _writeSemaphore.WaitAsync(linkedTokenSource.Token).ConfigureAwait(false);

            if (_isComplete)
                throw new InvalidOperationException("The stream has been marked as complete.");

            _current = pushedMessage;
            SafelyRelease(_readSemaphore);

            await _processedSemaphore.WaitAsync(linkedTokenSource.Token).ConfigureAwait(false);
            _writeSemaphore.Release();
        }

        /// <inheritdoc cref="IMessageStreamEnumerable.Abort" />
        public void Abort()
        {
            lock (_completeLock)
            {
                if (_isComplete)
                    return;

                _isComplete = true;
            }

            _abortCancellationTokenSource.Cancel();
        }

        /// <inheritdoc cref="IMessageStreamEnumerable.CompleteAsync" />
        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            lock (_completeLock)
            {
                if (_isComplete)
                    return;

                _isComplete = true;
            }

            await _writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            SafelyRelease(_readSemaphore);

            _writeSemaphore.Release();
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
        public IEnumerator<TMessage> GetEnumerator() =>
            GetEnumerable().GetEnumerator();

        /// <inheritdoc cref="IEnumerable.GetEnumerator" />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc cref="IAsyncEnumerable{T}.GetAsyncEnumerator" />
        public IAsyncEnumerator<TMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            GetAsyncEnumerable(cancellationToken).GetAsyncEnumerator(cancellationToken);

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
            {
                AsyncHelper.RunSynchronously(() => CompleteAsync());

                _readSemaphore.Dispose();
                _writeSemaphore.Dispose();
                _processedSemaphore.Dispose();
                _abortCancellationTokenSource.Dispose();
            }
        }

        private static void SafelyRelease(SemaphoreSlim semaphore)
        {
            lock (semaphore)
            {
                if (semaphore.CurrentCount == 0)
                    semaphore.Release();
            }
        }

        private IEnumerable<TMessage> GetEnumerable()
        {
            while (AsyncHelper.RunSynchronously(() => WaitForNextAsync(CancellationToken.None)))
            {
                if (_current == null)
                    continue;

                var currentMessage = (TMessage)_current.Message;
                yield return currentMessage;
            }
        }

        private async IAsyncEnumerable<TMessage> GetAsyncEnumerable(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (await WaitForNextAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_current == null)
                    continue;

                var currentMessage = (TMessage)_current.Message;
                yield return currentMessage;
            }
        }

        private async Task<bool> WaitForNextAsync(CancellationToken cancellationToken)
        {
            if (!_isFirstMessage)
            {
                _current = null;
                SafelyRelease(_processedSemaphore);
            }

            using var linkedTokenSource = LinkWithAbortCancellationTokenSource(cancellationToken);

            await _readSemaphore.WaitAsync(linkedTokenSource.Token).ConfigureAwait(false);

            _isFirstMessage = false;

            return _current != null;
        }

        private CancellationTokenSource LinkWithAbortCancellationTokenSource(CancellationToken cancellationToken) =>
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _abortCancellationTokenSource.Token);
    }
}
