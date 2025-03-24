// Copyright (c) 2025 Sergio Aquilini
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

    private Channel<T> _channel;

    private Channel<T> _overflowChannel; // Used to store messages when the main channel is full, to ensure nothing is lost

    private TaskCompletionSource<bool> _readTaskCompletionSource = new();

    private CancellationTokenSource _readCancellationTokenSource = new();

    private int _isReading; // Using an integer instead of a bool to be able to use it with Interlocked

    private bool _isDisposed;

    public ConsumerChannel(int capacity, string id, ISilverbackLogger logger)
    {
        _capacity = capacity;
        Id = id;
        _logger = logger;

        _channel = Channel.CreateBounded<T>(_capacity);
        _overflowChannel = Channel.CreateUnbounded<T>();
        SequenceStore = new SequenceStore(logger);
    }

    public string Id { get; }

    public ISequenceStore SequenceStore { get; private set; }

    public CancellationToken ReadCancellationToken => _readCancellationTokenSource.Token;

    public Task ReadTask => _readTaskCompletionSource.Task;

    public bool IsCompleted => _channel.Reader.Completion.IsCompleted;

    public void Complete() => _channel.Writer.TryComplete();

    public async ValueTask WriteAsync(T message, CancellationToken cancellationToken)
    {
        // Don't allow writing new messages until the overflow messages are processed
        while (_overflowChannel.Reader.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }

        await _channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask WriteOverflowAsync(T message) => _overflowChannel.Writer.WriteAsync(message, CancellationToken.None);

    public async ValueTask<T> ReadAsync()
    {
        if (_overflowChannel.Reader.TryRead(out T? overflowMessage))
            return overflowMessage;

        return await _channel.Reader.ReadAsync(ReadCancellationToken).ConfigureAwait(false);
    }

    public void Reset()
    {
        _channel.Writer.TryComplete();
        _overflowChannel.Writer.TryComplete();
        _channel = Channel.CreateBounded<T>(_capacity);
        _overflowChannel = Channel.CreateUnbounded<T>();
        SequenceStore.Dispose();
        SequenceStore = new SequenceStore(_logger);
    }

    public bool StartReading()
    {
        if (Interlocked.CompareExchange(ref _isReading, 1, 0) == 1)
            return false;

        if (_readCancellationTokenSource.IsCancellationRequested)
        {
            _readCancellationTokenSource.Dispose();
            _readCancellationTokenSource = new CancellationTokenSource();
        }

        if (_readTaskCompletionSource.Task.IsCompleted)
        {
            _readTaskCompletionSource = new TaskCompletionSource<bool>();
        }

        return true;
    }

    public async Task StopReadingAsync()
    {
        if (!_readCancellationTokenSource.IsCancellationRequested)
            await _readCancellationTokenSource.CancelAsync().ConfigureAwait(false);

        if (Volatile.Read(ref _isReading) == 0)
            _readTaskCompletionSource.TrySetResult(true);

        await _readTaskCompletionSource.Task.ConfigureAwait(false);
    }

    public async Task NotifyReadingStoppedAsync(bool hasThrown)
    {
        if (Interlocked.CompareExchange(ref _isReading, 0, 1) == 0)
            return;

        _readTaskCompletionSource.TrySetResult(!hasThrown);

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
}
