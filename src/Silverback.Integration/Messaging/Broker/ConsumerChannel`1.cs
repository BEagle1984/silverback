// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker;

internal class ConsumerChannel<T> : IConsumerChannel, IDisposable
{
    private readonly int _capacity;

    private readonly object _readingLock = new();

    private Channel<T> _channel;

    private TaskCompletionSource<bool> _readTaskCompletionSource = new();

    private CancellationTokenSource _readCancellationTokenSource = new();

    private bool _isDisposed;

    public ConsumerChannel(int capacity, string id)
    {
        _capacity = capacity;
        Id = id;

        _channel = CreateInnerChannel();
    }

    public string Id { get; }

    public CancellationToken ReadCancellationToken => _readCancellationTokenSource.Token;

    public Task ReadTask => _readTaskCompletionSource.Task;

    public bool IsReading { get; private set; }

    public void Complete() => _channel.Writer.TryComplete();

    public ValueTask WriteAsync(T message, CancellationToken cancellationToken) => _channel.Writer.WriteAsync(message, cancellationToken);

    public ValueTask<T> ReadAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAsync(cancellationToken);

    public void Reset()
    {
        _channel.Writer.Complete();
        _channel = CreateInnerChannel();
    }

    public bool StartReading()
    {
        lock (_readingLock)
        {
            if (IsReading)
                return false;

            IsReading = true;
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

    public Task StopReadingAsync()
    {
        if (!_readCancellationTokenSource.IsCancellationRequested)
            _readCancellationTokenSource.Cancel();

        lock (_readingLock)
        {
            if (!IsReading)
                _readTaskCompletionSource.TrySetResult(true);
        }

        return _readTaskCompletionSource.Task;
    }

    public void NotifyReadingStopped(bool hasThrown)
    {
        lock (_readingLock)
        {
            if (!IsReading)
                return;

            IsReading = false;
            _readTaskCompletionSource.TrySetResult(!hasThrown);
        }
    }

    public void Dispose() => Dispose(true);

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || _isDisposed)
            return;

        _readCancellationTokenSource.Dispose();

        _isDisposed = true;
    }

    private Channel<T> CreateInnerChannel() => Channel.CreateBounded<T>(_capacity);
}
