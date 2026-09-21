// Copyright (c) 2026 Sergio Aquilini
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

    private readonly System.Threading.Lock _channelLock = new();

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

    public CancellationToken ReadCancellationToken => _readCancellationTokenSource.Token;

    public Task ReadTask => _readTaskCompletionSource.Task;

    public bool IsCompleted => _channel.Reader.Completion.IsCompleted;

    /// <summary>
    ///     Gets the identity of the current channel buffers and processing state. Resetting the channel replaces this identity.
    /// </summary>
    public Guid InstanceId { get; private set; } = Guid.NewGuid();

    public ISequenceStore SequenceStore { get; private set; }

    public void Complete()
    {
        lock (_channelLock)
            _channel.Writer.TryComplete();
    }

    public async ValueTask WriteAsync(T message, CancellationToken cancellationToken)
    {
        (Channel<T> channel, Channel<T> overflowChannel) = GetChannels();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool overflowPending;
            lock (_channelLock)
            {
                // Older main-channel records precede overflow, which in turn precedes new writes
                overflowPending = overflowChannel.Reader.Count > 0;
                if (!overflowPending && channel.Writer.TryWrite(message))
                    return;
            }

            // Wait outside the lock and retry admission rather than enqueueing a pending write
            if (!await channel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
                throw new ChannelClosedException();

            if (overflowPending)
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
    }

    public ValueTask WriteOverflowAsync(T message)
    {
        lock (_channelLock)
            return _overflowChannel.Writer.WriteAsync(message, CancellationToken.None);
    }

    public async ValueTask<T> ReadAsync()
    {
        (Channel<T> channel, Channel<T> overflowChannel) = GetChannels();
        while (true)
        {
            ReadCancellationToken.ThrowIfCancellationRequested();

            lock (_channelLock)
            {
                // A canceled write enters overflow after records already buffered in the main channel
                if (channel.Reader.TryRead(out T? message))
                    return message;

                if (overflowChannel.Reader.TryRead(out T? overflowMessage))
                    return overflowMessage;
            }

            // Either queue can receive a message after the empty checks
            // Wait without consuming so the next iteration can select the oldest record under the lock
            using CancellationTokenSource waitCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ReadCancellationToken);

            try
            {
                Task<bool> messageAvailable = channel.Reader.WaitToReadAsync(waitCancellationTokenSource.Token).AsTask();
                Task<bool> overflowAvailable = overflowChannel.Reader.WaitToReadAsync(waitCancellationTokenSource.Token).AsTask();
                Task<bool> available = await Task.WhenAny(messageAvailable, overflowAvailable).ConfigureAwait(false);

                if (!await available.ConfigureAwait(false))
                    throw new ChannelClosedException();
            }
            finally
            {
                // Do not accumulate pending waits on the queue that didn't receive a message.
                await waitCancellationTokenSource.CancelAsync().ConfigureAwait(false);
            }
        }
    }

    public void Reset()
    {
        lock (_channelLock)
        {
            _channel.Writer.TryComplete();
            _overflowChannel.Writer.TryComplete();
            _channel = Channel.CreateBounded<T>(_capacity);
            _overflowChannel = Channel.CreateUnbounded<T>();
        }

        SequenceStore.Dispose();
        SequenceStore = new SequenceStore(_logger);
        InstanceId = Guid.NewGuid();
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

        await SequenceStore.AwaitAllProcessingAsync().ConfigureAwait(false);

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

    private (Channel<T> Channel, Channel<T> OverflowChannel) GetChannels()
    {
        lock (_channelLock)
            return (_channel, _overflowChannel);
    }
}
