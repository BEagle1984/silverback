// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Extended.Shared.Kafka;

namespace Silverback.Tests.Extended.Integration.Worker.Kafka;

internal sealed class ReconciliationSubscriber :
    IConsumerBehavior,
    IKafkaPartitionsAssignedCallback,
    IKafkaPartitionsRevokedCallback,
    IKafkaOffsetCommittedCallback,
    IDisposable
{
    private const string ChannelHeader = "x-stress-channel-id";

    private const string AssignmentHeader = "x-stress-assignment-epoch";

    private readonly IProducer<Null, byte[]> _receipts;

    private readonly string _prefix;

    private readonly string _member;

    private readonly ConcurrentDictionary<int, int> _epochs = new();

    private readonly ConditionalWeakTable<ISequenceStore, ChannelIdentity> _channels = [];

    private readonly ConcurrentDictionary<(int Partition, long ChannelId), byte> _announcedChannels = new();

    private long _nextChannelId;

    public ReconciliationSubscriber(string bootstrap, string prefix, string member)
    {
        _prefix = prefix;
        _member = member;
        _receipts = new ProducerBuilder<Null, byte[]>(new ProducerConfig
        {
            BootstrapServers = bootstrap,
            Acks = Acks.All,
            EnableIdempotence = true,
            LingerMs = 0,
            MessageTimeoutMs = 10000
        }).Build();
    }

    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.TransactionHandler - 1;

    public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
    {
        // Each channel owns a sequence store, replaced together with its buffers on reset
        ChannelIdentity channel = _channels.GetValue(context.SequenceStore, _ => new ChannelIdentity(Interlocked.Increment(ref _nextChannelId)));
        KafkaOffset offset = (KafkaOffset)context.Envelope.BrokerMessageIdentifier;
        context.Envelope.Headers.AddOrReplace(ChannelHeader, channel.Id);
        context.Envelope.Headers.AddOrReplace(AssignmentHeader, _epochs[offset.TopicPartition.Partition.Value]);

        await next(context, cancellationToken);
    }

    public Task OnMessageReceivedAsync(IInboundEnvelope<ReconciliationMessage> envelope) => ProcessAsync(envelope);

    public async Task OnBatchReceivedAsync(IAsyncEnumerable<IInboundEnvelope<ReconciliationMessage>> batch)
    {
        await foreach (IInboundEnvelope<ReconciliationMessage> envelope in batch)
        {
            await ProcessAsync(envelope);
        }
    }

    public IEnumerable<TopicPartitionOffset>? OnPartitionsAssigned(IReadOnlyCollection<TopicPartition> partitions, IKafkaConsumer consumer)
    {
        foreach (TopicPartition partition in partitions)
        {
            int epoch = _epochs.AddOrUpdate(partition.Partition.Value, 1, (_, previous) => previous + 1);
            WriteAsync(new ProcessingReceipt("assigned", _member, epoch, partition.Partition.Value, -1, -1)).GetAwaiter().GetResult();
        }

        Console.WriteLine($"ASSIGNED {string.Join(", ", partitions)}");

        return null;
    }

    public void OnPartitionsRevoked(IReadOnlyCollection<TopicPartitionOffset> partitions, IKafkaConsumer consumer)
    {
        foreach (TopicPartitionOffset partition in partitions)
        {
            WriteAsync(new ProcessingReceipt("revoked", _member, _epochs[partition.Partition.Value], partition.Partition.Value, -1, -1)).GetAwaiter().GetResult();
        }

        Console.WriteLine($"REVOKED {string.Join(", ", partitions)}");
    }

    public void OnOffsetsCommitted(CommittedOffsets offsets, IKafkaConsumer consumer)
    {
        if (offsets.Error.IsError)
            return;

        foreach (TopicPartitionOffsetError offset in offsets.Offsets)
        {
            if (offset.Error.IsError || offset.Offset.IsSpecial)
                continue;

            WriteAsync(new ProcessingReceipt("committed", _member, 0, offset.Partition.Value, -1, offset.Offset.Value)).GetAwaiter().GetResult();
        }
    }

    public void Dispose() => _receipts.Dispose();

    private async Task ProcessAsync(IInboundEnvelope<ReconciliationMessage> envelope)
    {
        KafkaOffset offset = envelope.GetKafkaOffset();
        int partition = offset.TopicPartition.Partition.Value;
        int epoch = envelope.Headers.GetValue<int>(AssignmentHeader) ?? throw new InvalidOperationException("Missing assignment epoch.");
        long channelId = envelope.Headers.GetValue<long>(ChannelHeader) ?? throw new InvalidOperationException("Missing processing channel.");

        if (envelope.Message!.Partition != partition)
            throw new InvalidOperationException("Message payload does not match its partition.");

        if (_announcedChannels.TryAdd((partition, channelId), 0))
            await WriteAsync(new ProcessingReceipt("channel-started", _member, epoch, partition, -1, offset.Offset.Value, channelId));

        await Task.Delay(25);
        await WriteAsync(new ProcessingReceipt("processed", _member, epoch, partition, envelope.Message.Sequence, offset.Offset.Value, channelId));

        if (envelope.Message.Sequence % 25 == 0)
            Console.WriteLine($"PROCESSED {partition}@{offset.Offset}");
    }

    private Task<DeliveryResult<Null, byte[]>> WriteAsync(ProcessingReceipt receipt) => _receipts.ProduceAsync(
        new TopicPartition(_prefix + "-receipts", receipt.Partition),
        new Message<Null, byte[]> { Value = JsonSerializer.SerializeToUtf8Bytes(receipt) });

    private sealed record ChannelIdentity(long Id);
}
