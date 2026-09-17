// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Messages;
using Silverback.Tests.Extended.Shared.Kafka;

namespace Silverback.Tests.Extended.Stress.Worker.Kafka;

internal sealed class ReconciliationSubscriber : IKafkaPartitionsAssignedCallback, IKafkaPartitionsRevokedCallback, IDisposable
{
    private readonly IProducer<Null, byte[]> _receipts;

    private readonly string _prefix;

    private readonly string _member;

    private readonly ConcurrentDictionary<int, int> _epochs = new();

    public ReconciliationSubscriber(string bootstrap, string prefix, string member)
    {
        _prefix = prefix;
        _member = member;
        _receipts = new ProducerBuilder<Null, byte[]>(new ProducerConfig
        {
            BootstrapServers = bootstrap, Acks = Acks.All, EnableIdempotence = true, LingerMs = 0, MessageTimeoutMs = 10000
        }).Build();
    }

    public Task OnMessageReceivedAsync(IInboundEnvelope<ReconciliationMessage> envelope) =>
        ProcessAsync(envelope, _epochs[envelope.GetKafkaOffset().TopicPartition.Partition.Value]);

    public async Task OnBatchReceivedAsync(IAsyncEnumerable<IInboundEnvelope<ReconciliationMessage>> batch)
    {
        Dictionary<int, int> epochs = new(_epochs);
        await foreach (IInboundEnvelope<ReconciliationMessage> envelope in batch)
            await ProcessAsync(envelope, epochs[envelope.GetKafkaOffset().TopicPartition.Partition.Value]);
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

    public void Dispose() => _receipts.Dispose();

    private async Task ProcessAsync(IInboundEnvelope<ReconciliationMessage> envelope, int epoch)
    {
        KafkaOffset offset = envelope.GetKafkaOffset();
        int partition = offset.TopicPartition.Partition.Value;
        if (envelope.Message!.Partition != partition)
            throw new InvalidOperationException("Message payload does not match its partition.");
        await Task.Delay(25);
        await WriteAsync(new ProcessingReceipt("processed", _member, epoch, partition, envelope.Message.Sequence, offset.Offset.Value));
        if (envelope.Message.Sequence % 25 == 0)
            Console.WriteLine($"PROCESSED {partition}@{offset.Offset}");
    }

    private Task<DeliveryResult<Null, byte[]>> WriteAsync(ProcessingReceipt receipt) => _receipts.ProduceAsync(
        new TopicPartition(_prefix + "-receipts", receipt.Partition),
        new Message<Null, byte[]> { Value = JsonSerializer.SerializeToUtf8Bytes(receipt) });
}
