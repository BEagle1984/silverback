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
using Silverback.Tests.Extended.Benchmarks.VersionComparison.V5_9_0.Common;

namespace Silverback.Tests.Extended.Benchmarks.VersionComparison.V5_9_0.MockedBroker;

/// <summary>
///     Measures the cost of consuming a ~200 B JSON message from the mocked Kafka broker, which stands in for the Confluent client, so
///     that the measurement covers the Silverback consumer pipeline only. The messages are produced while the consumer is stopped, then
///     the consumer is started and the benchmark waits until the subscriber received all of them.
/// </summary>
[Config(typeof(HostBenchmarkConfig))]
[MemoryDiagnoser]
public class MockedKafkaConsumeBenchmark : HostBenchmark
{
    private const int OperationsCount = 30000;

    private readonly MessageCounter _counter = new();

    [Params(false, true)]
    public bool TraceBaggage { get; set; }

    [Benchmark(Description = "Consume", OperationsPerInvoke = OperationsCount)]
    public async Task ConsumeAsync()
    {
        foreach (IConsumer consumer in Services.GetRequiredService<IConsumerCollection>())
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
                .Produce<SampleMessage>(endpoint => endpoint.ProduceTo("benchmark")))
            .AddConsumer(consumer => consumer
                .WithGroupId("benchmark")
                .AutoResetOffsetToEarliest()
                .Consume<SampleMessage>(endpoint => endpoint.ConsumeFrom("benchmark"))))
        .AddDelegateSubscriber<SampleMessage>(_ => _counter.Increment());

    protected override void OnHostStarted() => PrepareAsync().GetAwaiter().GetResult();

    private async Task PrepareAsync()
    {
        _counter.Reset(OperationsCount);

        // Wait for the (mocked) partitions assignment, otherwise its delay would be measured
        await Services.GetRequiredService<IKafkaTestingHelper>().WaitUntilConnectedAsync();

        foreach (IConsumer consumer in Services.GetRequiredService<IConsumerCollection>())
        {
            await consumer.StopAsync();
        }

        SampleMessage message = SampleMessage.Create();

        // The baggage is propagated to the message headers, which the consumer restores into its activity
        using Activity? activity = TraceBaggage ? BenchmarkActivity.StartWithBaggage() : null;

        for (int i = 0; i < OperationsCount; i++)
        {
            await Publisher.PublishAsync(message);
        }
    }
}
