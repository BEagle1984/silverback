// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
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
///     Measures the consumer pipeline against the mocked Kafka broker: the messages are produced while the consumer is stopped, then
///     the consumer is started and the benchmark waits until the subscriber received all of them.
/// </summary>
[Config(typeof(HostBenchmarkConfig))]
[MemoryDiagnoser]
public class KafkaConsumeBenchmark : HostBenchmarkBase
{
    private const int OperationsCount = 30000;

    private readonly MessageCounter _counter = new();

    [Params(ProducerHeadersKind.None, ProducerHeadersKind.TenStatic)]
    public ProducerHeadersKind Headers { get; set; }

    [Params(false, true)]
    public bool TraceBaggage { get; set; }

    [Benchmark(OperationsPerInvoke = OperationsCount)]
    public async Task Consume()
    {
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
                .Produce<BenchmarkMessage>(endpoint => endpoint
                    .ProduceTo("benchmark")
                    .SerializeAsJson()
                    .AddHeaders(Headers)))
            .AddConsumer(consumer => consumer
                .WithGroupId("benchmark")
                .AutoResetOffsetToEarliest()
                .Consume<BenchmarkMessage>(endpoint => endpoint.ConsumeFrom("benchmark"))))
        .AddDelegateSubscriber<BenchmarkMessage>(_ => _counter.Increment());

    protected override void OnHostStarted()
    {
        _counter.Reset(OperationsCount);

        // Wait for the (mocked) partitions assignment, otherwise its delay would be measured
        ScopedServices.GetRequiredService<IKafkaTestingHelper>().WaitUntilConnectedAsync().AsTask().SafeWait();

        IConsumerCollection consumers = ScopedServices.GetRequiredService<IConsumerCollection>();

        foreach (IConsumer consumer in consumers)
        {
            consumer.StopAsync().AsTask().SafeWait();
        }

        ProduceMessagesAsync().SafeWait();
    }

    private async Task ProduceMessagesAsync()
    {
        BenchmarkMessage message = BenchmarkMessage.Create(BenchmarkMessage.SmallPayloadLength);

        using Activity? activity = TraceBaggage ? BenchmarkActivity.StartWithBaggage() : null;

        for (int i = 0; i < OperationsCount; i++)
        {
            await Publisher.PublishAsync(message);
        }
    }
}
