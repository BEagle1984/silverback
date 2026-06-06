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

        await _processedSemaphore.WaitAsync(linkedTokenSource.Token).ConfigureAwait(false);
        _current = default;
        _hasCurrent = false;
        _writeSemaphore.Release();
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
        lock (_completeLock)
        {
            if (_completeState != CompleteState.NotComplete)
                return;

            _completeState = CompleteState.Complete;
        }

        await _writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        SafelyRelease(_readSemaphore);
        await _addCancellationTokenSource.CancelAsync().ConfigureAwait(false);
        _writeSemaphore.Release();
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
