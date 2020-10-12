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
    // TODO: Customizable back pressure?

    /// <inheritdoc cref="IMessageStreamEnumerable{TMessage}" />
    /// <remarks>
    ///     This implementation is not thread-safe.
    /// </remarks>
    internal class MessageStreamEnumerable<TMessage>
        : IMessageStreamEnumerable<TMessage>, IMessageStreamEnumerable, IDisposable
    {
        private readonly IMessageStreamProviderInternal? _ownerStreamProvider;

        private readonly SemaphoreSlim _writeSemaphore = new SemaphoreSlim(1, 1);

        private readonly SemaphoreSlim _readSemaphore = new SemaphoreSlim(0, 1);

        private readonly SemaphoreSlim _processedSemaphore = new SemaphoreSlim(0, 1);

        private readonly CancellationTokenSource _abortCancellationTokenSource = new CancellationTokenSource();

        private int _enumeratorsCount;

        private PushedMessage? _current;

        private bool _isFirstMessage = true;

        private bool _isComplete;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageStreamEnumerable{TMessage}" /> class.
        /// </summary>
        /// <param name="ownerStreamProvider">
        ///     The owner of the linked stream.
        /// </param>
        public MessageStreamEnumerable(IMessageStreamProviderInternal ownerStreamProvider)
        {
            _ownerStreamProvider = ownerStreamProvider;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageStreamEnumerable{TMessage}" /> class.
        /// </summary>
        public MessageStreamEnumerable()
        {
        }

        /// <inheritdoc cref="IMessageStreamEnumerable.MessageType" />
        public Type MessageType => typeof(TMessage);

        /// <inheritdoc cref="IMessageStreamEnumerable.PushAsync(PushedMessage,System.Threading.CancellationToken)" />
        [SuppressMessage("", "CA2000", Justification = Justifications.NewUsingSyntaxFalsePositive)]
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
            if (_isComplete)
                return;

            _isComplete = true;
            _abortCancellationTokenSource.Cancel();
        }

        /// <inheritdoc cref="IMessageStreamEnumerable.CompleteAsync" />
        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            if (_isComplete)
                return;

            _isComplete = true;

            await _writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            SafelyRelease(_readSemaphore);

            _writeSemaphore.Release();
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
        public IEnumerator<TMessage> GetEnumerator() =>
            EnumerateExclusively(() => GetEnumerable().GetEnumerator());

        /// <inheritdoc cref="IAsyncEnumerable{T}.GetAsyncEnumerator" />
        public IAsyncEnumerator<TMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            EnumerateExclusively(() => GetAsyncEnumerable(cancellationToken).GetAsyncEnumerator(cancellationToken));

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

        /// <inheritdoc cref="IEnumerable.GetEnumerator" />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private IEnumerable<TMessage> GetEnumerable()
        {
            // TODO: Check this pattern!
            while (AsyncHelper.RunSynchronously(() => WaitForNext(CancellationToken.None)))
            {
                if (_current == null)
                    continue;

                var currentMessage = (TMessage)_current.Message;
                yield return currentMessage;

                if (_ownerStreamProvider != null)
                    AsyncHelper.RunSynchronously(() => _ownerStreamProvider.NotifyStreamProcessedAsync(_current));
            }

            if (_ownerStreamProvider != null)
            {
                AsyncHelper.RunSynchronously(
                    () => _ownerStreamProvider.NotifyStreamEnumerationCompletedAsync(this));
            }
        }

        private async IAsyncEnumerable<TMessage> GetAsyncEnumerable(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (await WaitForNext(cancellationToken).ConfigureAwait(false))
            {
                if (_current == null)
                    continue;

                var currentMessage = (TMessage)_current.Message;
                yield return currentMessage;

                if (_ownerStreamProvider != null)
                    await _ownerStreamProvider.NotifyStreamProcessedAsync(_current).ConfigureAwait(false);
            }

            if (_ownerStreamProvider != null)
                await _ownerStreamProvider.NotifyStreamEnumerationCompletedAsync(this).ConfigureAwait(false);
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

        [SuppressMessage("", "CA2000", Justification = Justifications.NewUsingSyntaxFalsePositive)]
        private async Task<bool> WaitForNext(CancellationToken cancellationToken)
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
