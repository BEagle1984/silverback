// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IMessageStreamEnumerable{TMessage}" />
/// <remarks>
///     This implementation is not thread-safe.
/// </remarks>
internal sealed class MessageStreamEnumerable<TMessage> : IMessageStreamEnumerable<TMessage>, IMessageStreamEnumerable, IDisposable
{
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);

    private readonly SemaphoreSlim _readSemaphore = new(0, 1);

    private readonly SemaphoreSlim _processedSemaphore = new(0, 1);

    private readonly CancellationTokenSource _addCancellationTokenSource = new();

    private readonly System.Threading.Lock _completeLock = new();

    private TMessage? _current;

    private bool _hasCurrent;

    private bool _isFirstMessage = true;

    private CompleteState _completeState = CompleteState.NotComplete;

    private bool _isAbandoned;

    private Action<object?>? _onPullAction;

    private object? _onPullActionArgument;

    private enum CompleteState
    {
        NotComplete,

        Complete,

        Aborted,

        Detached
    }

    /// <inheritdoc cref="IMessageStreamEnumerable.PushAsync(object,Action{object},object,CancellationToken)" />
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "The lock is important to avoid multiple complete/abort, here is not important")]
    public async Task PushAsync(TMessage message, Action<object?>? onPullAction = null, object? onPullActionArgument = null, CancellationToken cancellationToken = default)
    {
        Check.NotNull(message, nameof(message));

        using CancellationTokenSource linkedTokenSource = LinkWithAddCancellationTokenSource(cancellationToken);

        await _writeSemaphore.WaitAsync(linkedTokenSource.Token).ConfigureAwait(false);

        // The subscriber of a detached stream won't pull any further message: acknowledge the push without waiting. The state is read
        // without the lock: the detach writes it and then releases the processed semaphore, unless the subscriber already released it
        // (the semaphore is full). A push reading the state before the write takes that count, after the detach released it or tried to,
        // and the next pushes acquire the write lock after that push, therefore reading the written state. The state never goes back to
        // NotComplete, so the count released by the detach can only acknowledge a push to the detached stream.
        if (_completeState == CompleteState.Detached)
        {
            ReleaseWriteLock();
            return;
        }

        // The stream was completed or aborted while this push waited for the write lock: give up, as if the push had been canceled. The
        // push gets here before the completion acquired the lock or, since the semaphore can grant it to a waiter whose token was canceled
        // in the meantime, after the completion or the abort. Otherwise, a subscriber still consuming the stream could receive the message
        // of a failing push, and if the subscriber stopped consuming it, nothing would acknowledge the push: both the push and the
        // completion would wait forever. The state is read without the lock. When the abandon or the completion acknowledged the previous
        // push (the subscriber stopped consuming the stream without pulling it), it did so after the state was written, and the previous
        // push then released the write lock acquired by this push, which therefore reads the written state, like a push acquiring the
        // lock released by the completion. Otherwise (e.g., the subscriber acknowledged the previous push by requesting the next message,
        // then stopped), a push reading a stale state proceeds as before: the subscriber pulls it, the abandon or the completion
        // acknowledges it, or the abort cancels it.
        if (_completeState is CompleteState.Complete or CompleteState.Aborted)
        {
            ReleaseWriteLock();
            throw new OperationCanceledException("The stream was completed or aborted.");
        }

        _current = message;
        _hasCurrent = true;
        _onPullAction = onPullAction;
        _onPullActionArgument = onPullActionArgument;
        SafelyRelease(_readSemaphore);

        try
        {
            await _processedSemaphore.WaitAsync(linkedTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Abort the stream, since the message might have been pulled already: its late processed signal could otherwise be taken
            // as the acknowledgement of the next pushed message
            Abort();
            throw;
        }
        finally
        {
            // Always release the write lock, otherwise CompleteAsync would wait for it forever
            _current = default;
            _hasCurrent = false;
            ReleaseWriteLock();
        }
    }

    /// <inheritdoc cref="IMessageStreamEnumerable.Abort" />
    public void Abort()
    {
        lock (_completeLock)
        {
            if (_completeState is CompleteState.Complete or CompleteState.Aborted)
                return;

            _completeState = CompleteState.Aborted;
        }

        _addCancellationTokenSource.Cancel();
    }

    /// <inheritdoc cref="IMessageStreamEnumerable.CompleteAsync" />
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        bool isAbandoned;

        lock (_completeLock)
        {
            if (_completeState is CompleteState.Complete or CompleteState.Aborted)
                return;

            _completeState = CompleteState.Complete;
            isAbandoned = _isAbandoned;
        }

        // Don't wait for the subscriber to pull the next message if it already stopped consuming the stream
        if (isAbandoned)
            AcknowledgePendingPush();

        await _writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        SafelyRelease(_readSemaphore);

        // Cancel before releasing the write lock, so that the waiting and the later pushes give up instead of taking a leftover
        // acknowledgment. A waiting push can still be granted the lock, but finds the stream complete (see PushAsync): it fails without
        // exposing its message to a subscriber still consuming the stream.
        await _addCancellationTokenSource.CancelAsync().ConfigureAwait(false);
        _writeSemaphore.Release();
    }

    /// <summary>
    ///     Signals that the subscriber stopped consuming the stream and will not pull any further message.
    /// </summary>
    /// <remarks>
    ///     Once the stream is complete (immediately, if it's already complete), the pending push is acknowledged as if the message had
    ///     been processed, since the next message will never be pulled. The pending push of a stream that isn't complete is instead left
    ///     to the abort or to the detach (see <see cref="Detach" />).
    /// </remarks>
    public void Abandon()
    {
        lock (_completeLock)
        {
            if (_isAbandoned)
                return;

            _isAbandoned = true;

            if (_completeState != CompleteState.Complete)
                return;
        }

        AcknowledgePendingPush();
    }

    /// <summary>
    ///     Signals that the subscriber stopped consuming the stream while the subscribers of the other streams of the same provider are still
    ///     enumerating them. The subscriber doesn't receive any further message: the pending push and the next ones are acknowledged without
    ///     waiting, as if the message had been processed.
    /// </summary>
    public void Detach()
    {
        lock (_completeLock)
        {
            if (_completeState != CompleteState.NotComplete)
                return;

            _completeState = CompleteState.Detached;
        }

        // Acknowledge the pending push, or the push that reads the state before it was written (see PushAsync)
        AcknowledgePendingPush();
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
    [MustDisposeResource]
    public IEnumerator<TMessage> GetEnumerator() => GetEnumerable().GetEnumerator();

    /// <inheritdoc cref="IAsyncEnumerable{T}.GetAsyncEnumerator" />
    public IAsyncEnumerator<TMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        GetAsyncEnumerable(cancellationToken).GetAsyncEnumerator(cancellationToken);

    Task IMessageStreamEnumerable.PushAsync(object message, Action<object?>? onPullAction, object? onPullActionArgument, CancellationToken cancellationToken) =>
        PushAsync((TMessage)message, onPullAction, onPullActionArgument, cancellationToken);

    /// <inheritdoc cref="IEnumerable.GetEnumerator" />
    [MustDisposeResource]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    {
        CompleteAsync().SafeWait();

        _readSemaphore.Dispose();
        _writeSemaphore.Dispose();
        _processedSemaphore.Dispose();
        _addCancellationTokenSource.Dispose();
    }

    private static void SafelyRelease(SemaphoreSlim semaphore)
    {
        try
        {
            semaphore.Release();
        }
        catch (SemaphoreFullException)
        {
            // Ignore
        }
    }

    private void ReleaseWriteLock()
    {
        try
        {
            _writeSemaphore.Release();
        }
        catch (ObjectDisposedException)
        {
            // Ignore: the stream can be disposed as soon as it's aborted (e.g., when the push is canceled)
        }
    }

    // Releases the pending push as the subscriber pulling the next message would do
    private void AcknowledgePendingPush()
    {
        try
        {
            _processedSemaphore.Release();
        }
        catch (SemaphoreFullException)
        {
            // Ignore: the push was already acknowledged (or no push is pending)
        }
        catch (ObjectDisposedException)
        {
            // Ignore: the stream was disposed after being completed
        }
    }

    private IEnumerable<TMessage> GetEnumerable()
    {
        while (WaitForNextAsync(CancellationToken.None).SafeWait())
        {
            // Read the message once: the push clears it when it's canceled (e.g., by the abort)
            TMessage? current = _current;

            if (!_hasCurrent || current == null)
                continue;

            _onPullAction?.Invoke(_onPullActionArgument);

            yield return current;
        }
    }

    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Reviewed")]
    private async IAsyncEnumerable<TMessage> GetAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await WaitForNextAsync(cancellationToken).ConfigureAwait(false))
        {
            // Read the message once: the push clears it when it's canceled (e.g., by the abort)
            TMessage? current = _current;

            if (!_hasCurrent || current == null)
                continue;

            _onPullAction?.Invoke(_onPullActionArgument);

            yield return current;
        }
    }

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "The lock is important to avoid multiple complete/abort, here is not important")]
    private async Task<bool> WaitForNextAsync(CancellationToken cancellationToken)
    {
        if (!_isFirstMessage)
        {
            _current = default;
            _hasCurrent = false;
            SafelyRelease(_processedSemaphore);
        }

        using CancellationTokenSource linkedTokenSource = LinkWithAddCancellationTokenSource(cancellationToken);

        try
        {
            await _readSemaphore.WaitAsync(linkedTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Handle the case where the token is canceled because the stream is complete, and don't throw but signal the end of the stream
            if (_completeState == CompleteState.Complete)
                return false;

            throw;
        }

        _isFirstMessage = false;

        if (_hasCurrent)
            return true;

        // The completion releases the subscriber without a message, to signal the end of the stream, and the detached stream has no
        // further message for it either. Otherwise, the push released its message but then withdrew it before it was read, typically
        // because it was canceled (e.g., by the abort): throw, as when the stream is aborted while waiting, instead of ending the stream
        // as if it was complete. The state is read without the lock: the completion writes it before releasing the subscriber and the
        // canceled push aborts the stream (unless the completion started first), while Complete and Aborted are final, so that an aborted
        // stream can't be read as complete.
        if (_completeState is not (CompleteState.Complete or CompleteState.Detached))
            throw new OperationCanceledException("The pushed message was withdrawn before being pulled (e.g., because the stream was aborted).");

        return false;
    }

    private CancellationTokenSource LinkWithAddCancellationTokenSource(CancellationToken cancellationToken) =>
        CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _addCancellationTokenSource.Token);
}
