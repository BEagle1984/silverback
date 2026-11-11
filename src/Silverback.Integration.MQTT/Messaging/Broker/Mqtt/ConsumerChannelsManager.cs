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
using MQTTnet;
using MQTTnet.Packets;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt;

internal class ConsumerChannelsManager : ConsumerChannelsManager<ConsumerChannel>
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private readonly MqttConsumer _consumer;

    private readonly ISilverbackLogger _logger;

    private readonly ConsumerChannel[] _channels;

    private readonly System.Threading.Lock _restartLock = new();

    private int _nextChannelIndex;

    private long _lastPosition;

    private int _stopGeneration; // Incremented by every stop, to skip the restarts after a rollback that were overtaken by a stop

    public ConsumerChannelsManager(MqttConsumer consumer, ISilverbackLogger logger)
        : base(consumer, logger)
    {
        _consumer = Check.NotNull(consumer, nameof(consumer));
        _logger = Check.NotNull(logger, nameof(logger));

        _channels =
        [
            .. Enumerable.Range(0, consumer.Configuration.MaxDegreeOfParallelism)
                .Select(index => new ConsumerChannel(consumer.Configuration.BackpressureLimit, index, logger))
        ];

        consumer.Client.MessageReceived.AddHandler(OnMessageReceivedAsync);
    }

    public void StartReading()
    {
        foreach (ConsumerChannel channel in _channels)
        {
            StartReading(channel);
        }
    }

    public void CompleteAll() => _channels.ForEach(channel => channel.Complete());

    public void ResetAll() => _channels.ForEach(channel => channel.Reset());

    /// <summary>
    ///     Restarts the reader of the channel after a rollback (see <see cref="ConsumerChannel.Rollback" />), to replay the rolled back
    ///     messages. Like the seek performed by Kafka, the restart aborts the sequences started in the meantime and stops the reader from
    ///     processing the later messages first.
    /// </summary>
    /// <param name="channel">
    ///     The channel to be restarted.
    /// </param>
    // The rollback is usually requested by the processing, which the stop awaits: never wait for the restart
    public void RestartReading(ConsumerChannel channel)
    {
        int stopGeneration;

        // Captured when the restart is requested: a restart delayed until after a stop (and maybe a start) must not handle the requests
        // added afterwards, which belong to another restart
        lock (_restartLock)
        {
            stopGeneration = _stopGeneration;
        }

        Task.Run(() => RestartReadingAsync(channel, stopGeneration)).FireAndForget();
    }

    protected override IEnumerable<ConsumerChannel> GetChannels() => _channels;

    protected override Task StopReadingAsync(ConsumerChannel channel)
    {
        // Incremented before the reading is canceled, to skip the restarts that would otherwise resume the stopped reader
        lock (_restartLock)
        {
            _stopGeneration++;
        }

        // Cancels the reading synchronously, before awaiting the reader
        Task stopReadingTask = base.StopReadingAsync(channel);

        // The stop takes over the pending restart requests: the messages to replay are processed when the reader is started again.
        // Cleared only after canceling the reading, since the pending requests keep the reader from taking the messages to replay
        // (into the sequences that the restart would abort).
        lock (_restartLock)
        {
            channel.ClearRestartRequests();
        }

        return stopReadingTask;
    }

    protected override async Task ReadChannelOnceAsync(ConsumerChannel channel)
    {
        channel.ReadCancellationToken.ThrowIfCancellationRequested();

        ConsumedApplicationMessage consumedMessage = await channel.ReadNextAsync().ConfigureAwait(false);

        // Clear the current activity to ensure we don't propagate the previous traceId
        Activity.Current = null;

        // The stop cancels the reading before the processing, but a reader must never process the message with the canceled
        // processing, which is rolled back right away
        ThrowAndPutBackIfCanceled(channel, consumedMessage);

        ConsumerPipelineContext context = await _consumer.HandleMessageAsync(consumedMessage, channel.SequenceStore).ConfigureAwait(false);

        ConsumerChannel.PendingState state = channel.Defer(consumedMessage, out int replayCount);

        // The message wasn't added to the sequence because it was aborted (or it was added right before the abort): await the
        // abort, which commits or rolls back the messages it tracks and schedules the later messages to be replayed
        if (state == ConsumerChannel.PendingState.Untracked && context.Sequence is { IsAborted: true } abortedSequence)
        {
            await abortedSequence.AwaitProcessingAsync(false).ConfigureAwait(false);
            state = channel.Defer(consumedMessage, out replayCount);
        }

        switch (state)
        {
            case ConsumerChannel.PendingState.Resolved:
                // Committed or rolled back: a rolled back message is replayed by the next read. An acknowledgment failure is rethrown,
                // to stop the reader and disconnect, since only a new connection replays the message.
                await consumedMessage.TaskCompletionSource.Task.ConfigureAwait(false);
                return;
            case ConsumerChannel.PendingState.Tracked:
                // The sequence the message was added to (e.g. a batch) commits or rolls it back. When stopping, the pending sequences
                // are aborted and their messages replayed when the reader restarts.
                ThrowAndPutBackIfCanceled(channel, consumedMessage);
                return;
            default:
                // When stopping, the message is processed again when the reader restarts
                ThrowAndPutBackIfCanceled(channel, consumedMessage);

                // No sequence commits the message: it was ignored by the sequence (no subscriber stream accepted it), skipped while a
                // sequence was pending (the commit would otherwise commit the pending sequence with Kafka) or left out of an aborted
                // sequence. With Kafka, it's committed by the next commit: acknowledge it, not to keep the delivery pending until the
                // client disconnects.
                if (context is { IsSequenceStart: true, Sequence: { IsPending: true } startedSequence })
                {
                    // The sequence started by the message is rolled back through its identifier (when the sequence is aborted, it's no
                    // longer pending and its context identifier is used): acknowledge it only after the sequence is processed, unless
                    // it's rolled back and replayed in the meantime
                    Task.Run(() => AcknowledgeAfterSequenceAsync(consumedMessage, replayCount, startedSequence)).FireAndForget();
                    return;
                }

                await AcknowledgeUntrackedAsync(consumedMessage).ConfigureAwait(false);
                return;
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private async Task AcknowledgeAfterSequenceAsync(ConsumedApplicationMessage consumedMessage, int replayCount, ISequence sequence)
    {
        try
        {
            await sequence.AwaitProcessingAsync(false).ConfigureAwait(false);

            if (consumedMessage.Channel.IsStillUntracked(consumedMessage, replayCount))
                await AcknowledgeUntrackedAsync(consumedMessage).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The acknowledgment failure disconnects the client (see MqttConsumer.CommitCoreAsync)
            _logger.LogConsumerFatalError(_consumer, ex);
        }
    }

    private ValueTask AcknowledgeUntrackedAsync(ConsumedApplicationMessage consumedMessage) =>
        _consumer.CommitAsync(new MqttMessageIdentifier(_consumer.Configuration.ClientId, consumedMessage.Id));

    private void ThrowAndPutBackIfCanceled(ConsumerChannel channel, ConsumedApplicationMessage consumedMessage)
    {
        if (!channel.ReadCancellationToken.IsCancellationRequested && !_consumer.IsProcessingCanceled && !channel.IsRestartPending)
            return;

        // The message is neither processed nor acknowledged: put it back to process it when restarting
        channel.PutBack(consumedMessage);
        throw new OperationCanceledException(channel.ReadCancellationToken);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private async Task RestartReadingAsync(ConsumerChannel channel, int stopGeneration)
    {
        try
        {
            // If the restart is requested again while stopping, the reader is stopped already: try again to start it
            bool isCompleted;

            do
            {
                isCompleted = await TryRestartReadingAsync(channel, stopGeneration).ConfigureAwait(false);
            }
            while (!isCompleted);
        }
        catch (Exception ex)
        {
            // The reconnection stops the consumer, which takes over the pending restart requests
            _logger.LogConsumerStartError(_consumer, ex);
            await _consumer.TriggerReconnectAsync().ConfigureAwait(false);
        }
    }

    // Returns false if the restart was requested again in the meantime and must be retried. A restart overtaken by a stop never touches
    // the reader or the restart requests, which belong to the stop (and to the reader started afterwards).
    private async Task<bool> TryRestartReadingAsync(ConsumerChannel channel, int stopGeneration)
    {
        // The generation is captured when the restart is requested, before checking the consumer status: a stop either incremented it
        // already (and is then visible as stopping) or increments it later, skipping the restart
        // The consumer is being stopped: the stop takes over the restart requests once it canceled the reading (see StopReadingAsync),
        // and the messages to replay are processed when the consumer is started again
        if (!_consumer.IsRunning || _consumer.IsProcessingCanceled)
            return true;

        int requestsCount;
        Task stopReadingTask;

        // Started under the lock, so that it can't stop a reader started after a stop that overtook the restart
        lock (_restartLock)
        {
            if (_stopGeneration != stopGeneration)
                return true;

            requestsCount = channel.GetRestartRequestsCount();

            // The requests were taken over by a stop that completed before the restart captured the generation (e.g. stopped and
            // started again in the meantime): don't stop the healthy reader
            if (requestsCount == 0)
                return true;

            // Aborts the pending sequences, which roll back their messages as well (requesting the restart again, if not stopped yet)
            stopReadingTask = channel.StopReadingAsync();
        }

        await stopReadingTask.ConfigureAwait(false);

        lock (_restartLock)
        {
            if (_stopGeneration != stopGeneration)
                return true;

            if (channel.RemoveRestartRequests(requestsCount))
                return false;

            StartReading(channel);
        }

        return true;
    }

    private ConsumerChannel GetChannel(MqttApplicationMessage applicationMessage)
    {
        if (_channels.Length == 1)
            return _channels[0];

        // All the chunks of a message must be processed in order by the same channel
        MqttUserProperty? chunkMessageIdProperty = _consumer.Configuration.AreHeadersSupported
            ? applicationMessage.UserProperties?.FirstOrDefault(property => property.Name == MqttMessageHeaders.ChunkMessageId)
            : null;

        if (chunkMessageIdProperty != null)
        {
            uint hash = (uint)StringComparer.Ordinal.GetHashCode(chunkMessageIdProperty.ReadValueAsString());
            return _channels[hash % (uint)_channels.Length];
        }

        ConsumerChannel channel = _channels[_nextChannelIndex];
        _nextChannelIndex = (_nextChannelIndex + 1) % _channels.Length;
        return channel;
    }

    private async ValueTask OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        // The messages are received one at a time: the position follows the order of the messages in each channel
        ConsumerChannel channel = GetChannel(eventArgs.ApplicationMessage);
        ConsumedApplicationMessage receivedMessage = new(eventArgs, channel, Interlocked.Increment(ref _lastPosition));

        _logger.LogConsuming(receivedMessage, _consumer);

        eventArgs.AutoAcknowledge = false;

        try
        {
            await channel.WriteAsync(receivedMessage, CancellationToken.None).ConfigureAwait(false);
        }
        catch (ChannelClosedException)
        {
            // Disconnecting closes the buffer to release the native receive callback without acknowledging the delivery
            eventArgs.ProcessingFailed = true;
        }
    }
}
