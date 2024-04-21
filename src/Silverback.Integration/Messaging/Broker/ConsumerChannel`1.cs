// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Sequences;

namespace Silverback.Messaging.Broker;

internal class ConsumerChannel<T> : IConsumerChannel, IDisposable
{
    private readonly int _capacity;

    private readonly ISilverbackLogger _logger;

    private readonly object _readingLock = new();

    private Channel<T> _channel;

    private TaskCompletionSource<bool> _readTaskCompletionSource = new();

    private CancellationTokenSource _readCancellationTokenSource = new();

    private bool _isReading;

    private bool _isDisposed;

    public ConsumerChannel(int capacity, string id, ISilverbackLogger logger)
    {
        _capacity = capacity;
        Id = id;
        _logger = logger;

        _channel = CreateInnerChannel();
        SequenceStore = new SequenceStore(logger);
    }

    public string Id { get; }

    public ISequenceStore SequenceStore { get; private set; }

    public CancellationToken ReadCancellationToken => _readCancellationTokenSource.Token;

    public Task ReadTask => _readTaskCompletionSource.Task;

    public bool IsCompleted => _channel.Reader.Completion.IsCompleted;

    public void Complete() => _channel.Writer.TryComplete();

    public ValueTask WriteAsync(T message, CancellationToken cancellationToken) => _channel.Writer.WriteAsync(message, cancellationToken);

    public ValueTask<T> ReadAsync() => _channel.Reader.ReadAsync(ReadCancellationToken);

    public void Reset()
    {
        _channel.Writer.TryComplete();
        _channel = CreateInnerChannel();
        SequenceStore.Dispose();
        SequenceStore = new SequenceStore(_logger);
    }

    public bool StartReading()
    {
        lock (_readingLock)
        {
            if (_isReading)
                return false;

            _isReading = true;
        }

        if (_readCancellationTokenSource.IsCancellationRequested)
        {
            _readCancellationTokenSource.Dispose();
            _readCancellationTokenSource = new CancellationTokenSource();
        }

        if (_readTaskCompletionSource.Task.IsCompleted)
            _readTaskCompletionSource = new TaskCompletionSource<bool>();

        return true;
    }

    public async Task StopReadingAsync()
    {
        if (!_readCancellationTokenSource.IsCancellationRequested)
        {
#if NETSTANDARD
            _readCancellationTokenSource.Cancel();
#else
            await _readCancellationTokenSource.CancelAsync().ConfigureAwait(false);
#endif
        }

        lock (_readingLock)
        {
            if (!_isReading)
                _readTaskCompletionSource.TrySetResult(true);
        }

        await _readTaskCompletionSource.Task.ConfigureAwait(false);
    }

    public async Task NotifyReadingStoppedAsync(bool hasThrown)
    {
        lock (_readingLock)
        {
            if (!_isReading)
                return;

            _isReading = false;

            _readTaskCompletionSource.TrySetResult(!hasThrown);
        }

        await SequenceStore.AbortAllAsync(SequenceAbortReason.ConsumerAborted).ConfigureAwait(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || _isDisposed)
            return;

        _readCancellationTokenSource.Dispose();
        SequenceStore.Dispose();

        _isDisposed = true;
    }

    private Channel<T> CreateInnerChannel() => Channel.CreateBounded<T>(_capacity);
}
