// Copyright (c) 2025 Sergio Aquilini
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

    private readonly CancellationTokenSource _abortCancellationTokenSource = new();

    private readonly object _completeLock = new();

    private TMessage? _current;

    private bool _hasCurrent;

    private bool _isFirstMessage = true;

    private bool _isComplete;

    private Action<object?>? _onPullAction;

    private object? _onPullActionArgument;

    Task IMessageStreamEnumerable.PushAsync(object message, Action<object?>? onPullAction, object? onPullActionArgument, CancellationToken cancellationToken) =>
        PushAsync((TMessage)message, onPullAction, onPullActionArgument, cancellationToken);

    /// <inheritdoc cref="IMessageStreamEnumerable.PushAsync(object,Action{object},object,CancellationToken)" />
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "The lock is important to avoid multiple complete/abort, here is not important")]
    public async Task PushAsync(TMessage message, Action<object?>? onPullAction = null, object? onPullActionArgument = null, CancellationToken cancellationToken = default)
    {
        Check.NotNull(message, nameof(message));

        using CancellationTokenSource linkedTokenSource = LinkWithAbortCancellationTokenSource(cancellationToken);

        await _writeSemaphore.WaitAsync(linkedTokenSource.Token).ConfigureAwait(false);

        if (_isComplete)
            throw new InvalidOperationException("The stream has been marked as complete.");

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
    [MustDisposeResource]
    public IEnumerator<TMessage> GetEnumerator() => GetEnumerable().GetEnumerator();

    /// <inheritdoc cref="IAsyncEnumerable{T}.GetAsyncEnumerator" />
    public IAsyncEnumerator<TMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        GetAsyncEnumerable(cancellationToken).GetAsyncEnumerator(cancellationToken);

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
        _abortCancellationTokenSource.Dispose();
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

    private async Task<bool> WaitForNextAsync(CancellationToken cancellationToken)
    {
        if (!_isFirstMessage)
        {
            _current = default;
            _hasCurrent = false;
            SafelyRelease(_processedSemaphore);
        }

        using CancellationTokenSource linkedTokenSource = LinkWithAbortCancellationTokenSource(cancellationToken);

        await _readSemaphore.WaitAsync(linkedTokenSource.Token).ConfigureAwait(false);

        _isFirstMessage = false;

        return _hasCurrent;
    }

    private CancellationTokenSource LinkWithAbortCancellationTokenSource(CancellationToken cancellationToken) =>
        CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _abortCancellationTokenSource.Token);
}
