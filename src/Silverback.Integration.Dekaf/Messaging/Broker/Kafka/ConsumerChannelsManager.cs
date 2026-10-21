// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

internal sealed class ConsumerChannelsManager : ConsumerChannelsManager<PartitionChannel>
{
    private static readonly TopicPartition AnyTopicPartition = new("single", Partition.Any);

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private readonly KafkaConsumer _consumer;

    private readonly IBrokerClientCallbacksInvoker _callbacksInvoker;

    private readonly ISilverbackLogger _logger;

    private readonly ConcurrentDictionary<TopicPartition, PartitionChannel> _channels = new();

    private readonly SemaphoreSlim _messagesLimiterSemaphoreSlim;

    private bool _isDisposed;

    public ConsumerChannelsManager(KafkaConsumer consumer, IBrokerClientCallbacksInvoker callbacksInvoker, ISilverbackLogger logger)
        : base(consumer, logger)
    {
        _consumer = Check.NotNull(consumer, nameof(consumer));
        _callbacksInvoker = Check.NotNull(callbacksInvoker, nameof(callbacksInvoker));
        _logger = Check.NotNull(logger, nameof(logger));

        _messagesLimiterSemaphoreSlim = new SemaphoreSlim(
            consumer.Configuration.MaxDegreeOfParallelism,
            consumer.Configuration.MaxDegreeOfParallelism);
    }

    public void StartReading(TopicPartition topicPartition) => StartReading(GetOrCreateChannel(topicPartition));

    public Task StopReadingAsync(TopicPartition topicPartition)
    {
        PartitionChannel? channel = GetChannel(topicPartition);

        if (channel == null)
            return Task.CompletedTask;

        Check.ThrowObjectDisposedIf(_isDisposed, this);

        return StopReadingAsync(channel);
    }

    public void Write(KafkaConsumedRecord consumedRecord, CancellationToken cancellationToken)
    {
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        PartitionChannel? channel = GetChannel(consumedRecord.TopicPartition);

        if (channel == null)
            return; // A stopped partition is replayed when rollback or reassignment starts a new reader

        _logger.LogConsumerTrace(
            _consumer,
            "Writing message ({Topic}[{Partition}]@{Offset}) to channel {Channel}",
            () => [consumedRecord.Topic, consumedRecord.Partition.Value, consumedRecord.Offset.Value, channel.Id]);

        try
        {
            // Blocking applies the backpressure to the poll, which runs synchronously on the consume loop thread
            channel.WriteAsync(consumedRecord, cancellationToken).SafeWait();
        }
        catch (OperationCanceledException)
        {
            try
            {
                channel.WriteOverflowAsync(consumedRecord).SafeWait();
            }
            catch (ChannelClosedException)
            {
                _logger.LogConsumerTrace(
                    _consumer,
                    "Failed to write message ({Topic}[{Partition}]@{Offset}) to overflow channel {Channel}",
                    () => [consumedRecord.Topic, consumedRecord.Partition.Value, consumedRecord.Offset.Value, channel.Id]);
            }
        }
    }

    public bool IsReading(TopicPartition topicPartition) =>
        GetChannel(topicPartition)?.ReadCancellationToken.IsCancellationRequested == false;

    internal PartitionChannel? GetChannel(TopicPartition topicPartition) =>
        _channels.GetValueOrDefault(GetTopicPartitionForChannelKey(topicPartition));

    internal async Task StopChannelAsync(PartitionChannel channel)
    {
        await base.StopReadingAsync(channel).ConfigureAwait(false);

        channel.Complete();

        // A late stop must not remove a replacement registered under the same partition key
        _channels.TryRemove(new KeyValuePair<TopicPartition, PartitionChannel>(channel.TopicPartition, channel));
    }

    protected override IEnumerable<PartitionChannel> GetChannels() => _channels.Values;

    protected override Task StopReadingAsync(PartitionChannel channel) => StopChannelAsync(channel);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || _isDisposed)
            return;

        _messagesLimiterSemaphoreSlim.Dispose();

        _isDisposed = true;
    }

    protected override async Task ReadChannelOnceAsync(PartitionChannel channel)
    {
        channel.ReadCancellationToken.ThrowIfCancellationRequested();

        KafkaConsumedRecord consumedRecord = await channel.ReadAsync().ConfigureAwait(false);

        channel.ReadCancellationToken.ThrowIfCancellationRequested();

        if (consumedRecord.IsPartitionEof)
        {
            _logger.LogEndOfPartition(consumedRecord, _consumer);
            _callbacksInvoker.Invoke<IKafkaPartitionEofCallback>(handler => handler.OnEndOfTopicPartitionReached(
                consumedRecord.TopicPartition,
                _consumer));

            return;
        }

        bool semaphoreWaited = false;

        if (_channels.Count > _consumer.Configuration.MaxDegreeOfParallelism)
        {
            await _messagesLimiterSemaphoreSlim.WaitAsync(channel.ReadCancellationToken).ConfigureAwait(false);
            semaphoreWaited = true;
        }

        try
        {
            await _consumer.HandleMessageAsync(consumedRecord, channel.SequenceStore, channel.InstanceId).ConfigureAwait(false);
        }
        finally
        {
            if (semaphoreWaited)
                _messagesLimiterSemaphoreSlim.Release();
        }
    }

    private PartitionChannel GetOrCreateChannel(TopicPartition topicPartition) =>
        GetChannel(topicPartition) ?? _channels.GetOrAdd(
            GetTopicPartitionForChannelKey(topicPartition),
            static (keyTopicPartition, args) =>
                new PartitionChannel(args.BackpressureLimit, keyTopicPartition, args.Logger),
            (_consumer.Configuration.BackpressureLimit, Logger: _logger));

    private TopicPartition GetTopicPartitionForChannelKey(TopicPartition topicPartition) =>
        _consumer.Configuration.ProcessPartitionsIndependently ? topicPartition : AnyTopicPartition;
}
