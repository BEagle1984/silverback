// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

internal abstract class ConsumerChannelsManager<TChannel> : IDisposable
    where TChannel : IConsumerChannel
{
    private readonly IConsumer _consumer;

    private readonly ISilverbackLogger _logger;

    private readonly System.Threading.Lock _stopGenerationLock = new();

    private int _stopGeneration; // Incremented by every stop, to skip the restarts deferred before it

    private bool _isDisposed;

    protected ConsumerChannelsManager(IConsumer consumer, ISilverbackLogger logger)
    {
        _consumer = Check.NotNull(consumer, nameof(consumer));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    public Task Stopping => Task.WhenAll(GetChannels().Select(channel => channel.ReadTask));

    public Task StopReadingAsync() => Task.WhenAll(GetChannels().Select(StopReadingAsync));

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected abstract IEnumerable<TChannel> GetChannels();

    protected void StartReading(TChannel channel)
    {
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        // Clear the current activity to ensure we don't propagate the previous traceId (e.g. when restarting because of a rollback)
        Activity.Current = null;

        // Captured before checking the reading state, so that any stop requested after this call skips the deferred restart
        int stopGeneration = GetStopGeneration();

        if (channel.ReadCancellationToken.IsCancellationRequested && !channel.ReadTask.IsCompleted)
        {
            _logger.LogChannelProcessingLoopStartupDeferred(_consumer, channel);

            // If the cancellation is still pending, await it and restart after successful stop
            StartReadingAfterStopAsync(channel, stopGeneration).FireAndForget();

            return;
        }

        if (!channel.StartReading())
        {
            _logger.LogChannelProcessingLoopStartupIgnored(_consumer, channel);

            return;
        }

        // The loop outlives the caller, which may be processing the message being rolled back: it must not inherit its async context,
        // or the objects stored in the SilverbackContext would leak into the messages read by the new loop
        using (ExecutionContext.SuppressFlow())
        {
            Task.Run(() => ReadChannelAsync(channel)).FireAndForget();
        }
    }

    [SuppressMessage("Usage", "VSTHRD202:Return Task directly instead of awaiting", Justification = "The disposed check must fault the returned task instead of throwing synchronously")]
    protected virtual async Task StopReadingAsync(TChannel channel)
    {
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        // Incremented before canceling the reading, to skip the restarts deferred until now
        lock (_stopGenerationLock)
            _stopGeneration++;

        _logger.LogChannelProcessingLoopStopping(_consumer, channel);

        await channel.StopReadingAsync().ConfigureAwait(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || _isDisposed)
            return;

        StopReadingAsync().SafeWait();
        GetChannels().OfType<IDisposable>().ForEach(channel => channel.Dispose());

        _logger.LogAllChannelsDisposed(_consumer);

        _isDisposed = true;
    }

    protected abstract Task ReadChannelOnceAsync(TChannel channel);

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private async Task ReadChannelAsync(TChannel channel)
    {
        bool hasThrown = false;

        try
        {
            _logger.LogChannelProcessingLoopStarting(_consumer, channel);

            while (!channel.ReadCancellationToken.IsCancellationRequested)
            {
                _logger.LogChannelReading(_consumer, channel);

                await ReadChannelOnceAsync(channel).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogChannelProcessingLoopCanceled(_consumer, channel);
        }
        catch (ChannelClosedException)
        {
            // Ignore the ChannelClosedException as it might be thrown because of a race condition during retry and in any case
            // it means that operation is being canceled
            _logger.LogChannelProcessingLoopClosed(_consumer, channel);
        }
        catch (Exception ex)
        {
            if (ex is not ConsumerPipelineFatalException)
                _logger.LogConsumerFatalError(_consumer, ex);

            hasThrown = true;
        }

        _logger.LogChannelProcessingLoopExited(_consumer, channel);

        // Notify exactly once, since the channel can be restarted as soon as it's notified and the new loop must not be notified by this
        // one. Notify before disconnecting, since the disconnection stops the channel and awaits its read task.
        await channel.NotifyReadingStoppedAsync(hasThrown).ConfigureAwait(false);

        if (hasThrown)
            await _consumer.Client.DisconnectAsync().ConfigureAwait(false);
    }

    private int GetStopGeneration()
    {
        lock (_stopGenerationLock)
            return _stopGeneration;
    }

    private async Task StartReadingAfterStopAsync(TChannel channel, int stopGeneration)
    {
        await channel.ReadTask.ConfigureAwait(false);

        // A stop requested while the restart was pending wins, otherwise the restarted loop would keep reading while stopped. The
        // check and the restart are atomic with respect to the stops, since a later stop must find the new read token to cancel it.
        lock (_stopGenerationLock)
        {
            if (_stopGeneration != stopGeneration)
            {
                _logger.LogChannelProcessingLoopStartupSkipped(_consumer, channel);

                return;
            }

            StartReading(channel);
        }
    }
}
