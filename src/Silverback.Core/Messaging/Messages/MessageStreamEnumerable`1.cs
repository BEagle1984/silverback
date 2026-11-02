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

        Aborted
    }

    /// <inheritdoc cref="IMessageStreamEnumerable.PushAsync(object,Action{object},object,CancellationToken)" />
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "The lock is important to avoid multiple complete/abort, here is not important")]
    public async Task PushAsync(TMessage message, Action<object?>? onPullAction = null, object? onPullActionArgument = null, CancellationToken cancellationToken = default)
    {
        Check.NotNull(message, nameof(message));

        using CancellationTokenSource linkedTokenSource = LinkWithAddCancellationTokenSource(cancellationToken);

        await _writeSemaphore.WaitAsync(linkedTokenSource.Token).ConfigureAwait(false);

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
            if (_completeState != CompleteState.NotComplete)
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
            if (_completeState != CompleteState.NotComplete)
                return;

            _completeState = CompleteState.Complete;
            isAbandoned = _isAbandoned;
        }

        // Don't wait for the subscriber to pull the next message if it already stopped consuming the stream
        if (isAbandoned)
            AcknowledgePendingPush();

        await _writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        SafelyRelease(_readSemaphore);

        // Cancel before releasing the write lock: no further push must acquire it and take a leftover acknowledgement
        await _addCancellationTokenSource.CancelAsync().ConfigureAwait(false);
        _writeSemaphore.Release();
    }

    /// <summary>
    ///     Signals that the subscriber stopped consuming the stream and will not pull any further message.
    /// </summary>
    /// <remarks>
    ///     Once the stream is complete (immediately, if it's already complete), the pending push is acknowledged as if the message had
    ///     been processed, since the next message will never be pulled. The pending push of a stream that isn't complete is instead left
    ///     to the abort.
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
            if (!_hasCurrent || _current == null)
                continue;

            _onPullAction?.Invoke(_onPullActionArgument);

            yield return _current;
        }
    }

    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Reviewed")]
    private async IAsyncEnumerable<TMessage> GetAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await WaitForNextAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!_hasCurrent || _current == null)
                continue;

            _onPullAction?.Invoke(_onPullActionArgument);

            yield return _current;
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

        return _hasCurrent;
    }

    private CancellationTokenSource LinkWithAddCancellationTokenSource(CancellationToken cancellationToken) =>
        CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _addCancellationTokenSource.Token);
}
