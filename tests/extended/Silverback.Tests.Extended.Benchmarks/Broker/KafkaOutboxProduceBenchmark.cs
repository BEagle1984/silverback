// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Extended.Benchmarks.Common;

namespace Silverback.Tests.Extended.Benchmarks.Broker;

/// <summary>
///     Measures storing the messages to the in-memory outbox (no outbox worker is running, so only the produce strategy is measured):
///     one message at a time and in batches of 100 messages.
/// </summary>
[Config(typeof(HostBenchmarkConfig))]
[MemoryDiagnoser]
public class KafkaOutboxProduceBenchmark : HostBenchmarkBase
{
    private const int BatchSize = 100;

    private const int OperationsCount = 30000;

    private static readonly BenchmarkMessage Message = BenchmarkMessage.Create(BenchmarkMessage.SmallPayloadLength);

    private static readonly BenchmarkMessage[] Batch = [.. Enumerable.Repeat(Message, BatchSize)];

    [Benchmark(OperationsPerInvoke = OperationsCount)]
    public async Task PublishSingleMessage()
    {
        for (int i = 0; i < OperationsCount; i++)
        {
            await Publisher.PublishAsync(Message);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsCount)]
    public async Task PublishBatchAsCollection()
    {
        for (int i = 0; i < OperationsCount / BatchSize; i++)
        {
            await Publisher.PublishAsync(Batch);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsCount)]
    public async Task PublishBatchAsEnumerable()
    {
        for (int i = 0; i < OperationsCount / BatchSize; i++)
        {
            await Publisher.PublishAsync(GetBatch());
        }
    }

    protected override void ConfigureSilverback(SilverbackBuilder silverbackBuilder) => silverbackBuilder
        .WithConnectionToMessageBroker(options => options
            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
            .AddInMemoryOutbox())
        .AddKafkaClients(clients => clients
            .WithBootstrapServers("PLAINTEXT://benchmark")
            .AddProducer(producer => producer
                .Produce<BenchmarkMessage>(
                    "benchmark-outbox-endpoint",
                    endpoint => endpoint
                        .ProduceTo("benchmark")
                        .SerializeAsJson()
                        .StoreToOutbox(outbox => outbox.UseMemory()))));

    // A lazy sequence, so that the publisher cannot treat it as a collection
    private static IEnumerable<BenchmarkMessage> GetBatch()
    {
        foreach (BenchmarkMessage message in Batch)
        {
            yield return message;
        }
    }
}
