// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
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
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        PartitionChannel? channel = GetChannel(topicPartition);

        if (channel == null)
            return Task.CompletedTask;

        return StopReadingAsync(channel);
    }

    public void Reset(TopicPartition topicPartition)
    {
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        GetChannel(topicPartition)?.Reset();
    }

    public void ResetAll()
    {
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        _channels.Values.ForEach(channel => channel.Reset());
    }

    public void Write(ConsumeResult<byte[]?, byte[]?> consumeResult, CancellationToken cancellationToken)
    {
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        PartitionChannel channel = GetOrCreateChannel(consumeResult.TopicPartition);

        _logger.LogConsumerLowLevelTrace(
            _consumer,
            "Writing message ({topic}[{partition}]@{offset}) to channel {channel}.",
            () =>
            [
                consumeResult.Topic,
                consumeResult.Partition.Value,
                consumeResult.Offset.Value,
                channel.Id
            ]);

        // There's unfortunately no async version of Confluent.Kafka.IConsumer.Consume() so we need to run
        // synchronously to stay within a single long-running thread with the Consume loop.
        channel.WriteAsync(consumeResult, cancellationToken).SafeWait(cancellationToken);
    }

    public bool IsReading(TopicPartition topicPartition) =>
        GetChannel(topicPartition)?.ReadCancellationToken.IsCancellationRequested != true;

    protected override IEnumerable<PartitionChannel> GetChannels() => _channels.Values;

    protected override async Task StopReadingAsync(PartitionChannel channel)
    {
        await base.StopReadingAsync(channel).ConfigureAwait(false);

        channel.Complete();
        _channels.TryRemove(channel.TopicPartition, out _);
    }

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

        ConsumeResult<byte[]?, byte[]?> consumeResult = await channel.ReadAsync().ConfigureAwait(false);

        channel.ReadCancellationToken.ThrowIfCancellationRequested();

        if (consumeResult.IsPartitionEOF)
        {
            _logger.LogEndOfPartition(consumeResult, _consumer);
            _callbacksInvoker.Invoke<IKafkaPartitionEofCallback>(
                handler => handler.OnEndOfTopicPartitionReached(
                    consumeResult.TopicPartition,
                    _consumer));
            return;
        }

        if (_channels.Count > _consumer.Configuration.MaxDegreeOfParallelism)
        {
            await _messagesLimiterSemaphoreSlim.WaitAsync(channel.ReadCancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            await _consumer.HandleMessageAsync(consumeResult.Message, consumeResult.TopicPartitionOffset, channel.SequenceStore)
                .ConfigureAwait(false);
        }
        finally
        {
            if (_messagesLimiterSemaphoreSlim.CurrentCount < _consumer.Configuration.MaxDegreeOfParallelism)
                _messagesLimiterSemaphoreSlim.Release();
        }
    }

    private PartitionChannel GetOrCreateChannel(TopicPartition topicPartition) =>
        GetChannel(topicPartition) ?? _channels.GetOrAdd(
            GetTopicPartitionForChannelKey(topicPartition),
            static (keyTopicPartition, args) =>
                new PartitionChannel(args.BackpressureLimit, keyTopicPartition, args.Logger),
            (_consumer.Configuration.BackpressureLimit, Logger: _logger));

    private PartitionChannel? GetChannel(TopicPartition topicPartition) =>
        _channels.GetValueOrDefault(GetTopicPartitionForChannelKey(topicPartition));

    private TopicPartition GetTopicPartitionForChannelKey(TopicPartition topicPartition) =>
        _consumer.Configuration.ProcessPartitionsIndependently ? topicPartition : AnyTopicPartition;
}
