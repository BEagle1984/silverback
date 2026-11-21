// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Security.Cryptography;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Testing;
using Silverback.Tests.Extended.Benchmarks.Common;
using Silverback.Util;

namespace Silverback.Tests.Extended.Benchmarks.Broker;

/// <summary>
///     Measures producing or consuming a ~64 KB JSON message (via the mocked Kafka broker) as it is, split into 1 KB chunks, AES
///     encrypted, or both.
/// </summary>
/// <remarks>
///     The two sides are measured apart. When producing, the consumer is stopped. When consuming, the messages are produced in the
///     iteration setup and the benchmark only starts the consumer and waits until everything is consumed. This way the polling sleep of
///     the mocked consumer (about 15 ms on Windows, when no message is available) never ends up in the measured region.
///     BenchmarkDotNet may report a few exceptions per run: they are the first-chance <see cref="System.OperationCanceledException" />
///     thrown by the consumer channel while the mocked consumers are being stopped or reassigned, which is inherent to the code under
///     test.
/// </remarks>
[Config(typeof(HostBenchmarkConfig))]
[MemoryDiagnoser]
public class KafkaChunkingAndEncryptionBenchmark : HostBenchmarkBase
{
    private const int OperationsCount = 500;

    private const int ChunkSize = 1024;

    private static readonly BenchmarkMessage Message = BenchmarkMessage.Create(BenchmarkMessage.LargePayloadLength);

    private static readonly byte[] EncryptionKey = RandomNumberGenerator.GetBytes(32);

    private readonly MessageCounter _counter = new();

    public enum OperationKind
    {
        Produce,

        Consume
    }

    public enum ProcessingKind
    {
        Plain,

        Chunked,

        Encrypted,

        ChunkedAndEncrypted
    }

    [Params(OperationKind.Produce, OperationKind.Consume)]
    public OperationKind Operation { get; set; }

    [Params(ProcessingKind.Plain, ProcessingKind.Chunked, ProcessingKind.Encrypted, ProcessingKind.ChunkedAndEncrypted)]
    public ProcessingKind Processing { get; set; }

    private bool IsChunked => Processing is ProcessingKind.Chunked or ProcessingKind.ChunkedAndEncrypted;

    private bool IsEncrypted => Processing is ProcessingKind.Encrypted or ProcessingKind.ChunkedAndEncrypted;

    [Benchmark(OperationsPerInvoke = OperationsCount)]
    public async Task Run()
    {
        if (Operation == OperationKind.Produce)
        {
            await ProduceMessagesAsync();
            return;
        }

        foreach (IConsumer consumer in ScopedServices.GetRequiredService<IConsumerCollection>())
        {
            await consumer.StartAsync();
        }

        await _counter.WaitAsync();
    }

    protected override void ConfigureSilverback(SilverbackBuilder silverbackBuilder) => silverbackBuilder
        .WithConnectionToMessageBroker(options => options.AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
        .AddKafkaClients(clients => clients
            .WithBootstrapServers("PLAINTEXT://benchmark")
            .AddProducer(producer => producer
                .Produce<BenchmarkMessage>(endpoint =>
                {
                    endpoint.ProduceTo("benchmark").SerializeAsJson();

                    if (IsChunked)
                        endpoint.EnableChunking(ChunkSize);

                    if (IsEncrypted)
                        endpoint.EncryptUsingAes(EncryptionKey);
                }))
            .AddConsumer(consumer => consumer
                .WithGroupId("benchmark")
                .AutoResetOffsetToEarliest()
                .Consume<BenchmarkMessage>(endpoint =>
                {
                    endpoint.ConsumeFrom("benchmark");

                    if (IsEncrypted)
                        endpoint.DecryptUsingAes(EncryptionKey);
                })))
        .AddDelegateSubscriber<BenchmarkMessage>(_ => _counter.Increment());

    protected override void OnHostStarted()
    {
        _counter.Reset(OperationsCount);

        // Wait for the (mocked) partitions assignment, otherwise its delay would be measured
        ScopedServices.GetRequiredService<IKafkaTestingHelper>().WaitUntilConnectedAsync().AsTask().SafeWait();

        foreach (IConsumer consumer in ScopedServices.GetRequiredService<IConsumerCollection>())
        {
            consumer.StopAsync().AsTask().SafeWait();
        }

        if (Operation == OperationKind.Consume)
            ProduceMessagesAsync().SafeWait();
    }

    private async Task ProduceMessagesAsync()
    {
        for (int i = 0; i < OperationsCount; i++)
        {
            await Publisher.PublishAsync(Message);
        }
    }
}
