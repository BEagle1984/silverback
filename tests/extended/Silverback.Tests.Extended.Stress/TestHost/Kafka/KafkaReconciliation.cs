// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Silverback.Tests.Extended.Shared.Kafka;

namespace Silverback.Tests.Extended.Stress.TestHost.Kafka;

public sealed class KafkaReconciliation
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly ContainerTestRun _run;

    private readonly Dictionary<(int Partition, long Offset), ReconciliationMessage> _produced = [];

    public KafkaReconciliation(ContainerTestRun run)
    {
        _run = run;
    }

    public async Task ProduceAsync(int partitions, int count)
    {
        using IAdminClient admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = KafkaFixture.BootstrapServers }).Build();
        await admin.CreateTopicsAsync(new[] { "-records", "-receipts" }.Select(suffix => new TopicSpecification
        {
            Name = _run.Prefix + suffix,
            NumPartitions = partitions,
            ReplicationFactor = 1
        }));

        using IProducer<Null, byte[]> producer = new ProducerBuilder<Null, byte[]>(new ProducerConfig
        {
            BootstrapServers = KafkaFixture.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            LingerMs = 0
        }).Build();

        for (int sequence = 0; sequence < count; sequence++)
        {
            for (int partition = 0; partition < partitions; partition++)
            {
                ReconciliationMessage message = new(partition, sequence);
                DeliveryResult<Null, byte[]> result = await producer.ProduceAsync(
                    new TopicPartition(_run.Prefix + "-records", partition),
                    new Message<Null, byte[]> { Value = JsonSerializer.SerializeToUtf8Bytes(message) });

                _produced[(partition, result.Offset.Value)] = message;
            }
        }

        await File.WriteAllTextAsync(
            Path.Combine(_run.Artifacts, "produced.json"),
            JsonSerializer.Serialize(_produced.Select(pair => new { pair.Key.Partition, pair.Key.Offset, pair.Value.Sequence })));
    }

    public async Task WaitForCommitAsync(TimeSpan timeout)
    {
        using IConsumer<Ignore, byte[]> inspector = CreateConsumer(_run.Prefix + "-group");
        Dictionary<int, long> ends = _produced.Keys.GroupBy(key => key.Partition)
            .ToDictionary(group => group.Key, group => group.Max(key => key.Offset) + 1);

        TopicPartition[] partitions = [.. ends.Keys.Select(partition => new TopicPartition(_run.Prefix + "-records", partition))];
        DateTime deadline = DateTime.UtcNow + timeout;
        List<TopicPartitionOffset> committed = [];

        while (DateTime.UtcNow < deadline)
        {
            committed = inspector.Committed(partitions, TimeSpan.FromSeconds(5));

            if (committed.All(offset => offset.Offset.Value == ends[offset.Partition.Value]))
            {
                await File.WriteAllTextAsync(
                    Path.Combine(_run.Artifacts, "committed.json"),
                    JsonSerializer.Serialize(committed.Select(offset => new { Partition = offset.Partition.Value, Offset = offset.Offset.Value })));

                return;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"Offsets did not drain: {string.Join(", ", committed)}. Artifacts: {_run.Artifacts}");
    }

    public async Task<ReconciliationReport> VerifyAsync()
    {
        using IConsumer<Ignore, byte[]> reader = CreateConsumer(_run.Prefix + "-verifier");
        Dictionary<int, long> ends = _produced.Keys.Select(key => key.Partition).Distinct().ToDictionary(
            partition => partition,
            partition => reader.QueryWatermarkOffsets(new TopicPartition(_run.Prefix + "-receipts", partition), TimeSpan.FromSeconds(5)).High.Value);

        reader.Assign(ends.Keys.Select(partition => new TopicPartitionOffset(_run.Prefix + "-receipts", partition, Offset.Beginning)));

        HashSet<int> finished = [.. ends.Where(pair => pair.Value == 0).Select(pair => pair.Key)];
        KafkaReconciliationVerifier verifier = new(_produced);
        List<ProcessingReceipt> journal = [];
        DateTime deadline = DateTime.UtcNow.AddSeconds(30);

        while (finished.Count < ends.Count)
        {
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException("Receipt journal did not reach its final watermark.");

            ConsumeResult<Ignore, byte[]>? result = reader.Consume(TimeSpan.FromMilliseconds(250));

            if (result == null)
                continue;

            ProcessingReceipt receipt = JsonSerializer.Deserialize<ProcessingReceipt>(result.Message.Value)!;
            journal.Add(receipt);
            verifier.Observe(receipt, result.Partition.Value);

            if (result.Offset.Value + 1 >= ends[result.Partition.Value])
                finished.Add(result.Partition.Value);
        }

        ReconciliationReport report = verifier.GetReport();
        await File.WriteAllTextAsync(Path.Combine(_run.Artifacts, "receipts.json"), JsonSerializer.Serialize(journal));
        await File.WriteAllTextAsync(
            Path.Combine(_run.Artifacts, "reconciliation.json"),
            JsonSerializer.Serialize(report, JsonOptions));

        return report;
    }

    private static IConsumer<Ignore, byte[]> CreateConsumer(string group) => new ConsumerBuilder<Ignore, byte[]>(new ConsumerConfig
    {
        BootstrapServers = KafkaFixture.BootstrapServers,
        GroupId = group,
        EnableAutoCommit = false
    }).Build();
}
