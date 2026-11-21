// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Extended.Benchmarks.VersionComparison.V5_9_0.Common;

namespace Silverback.Tests.Extended.Benchmarks.VersionComparison.V5_9_0.MockedBroker;

/// <summary>
///     Measures the cost of publishing a ~200 B JSON message to the mocked Kafka broker, which stands in for the Confluent client, so
///     that the measurement covers the Silverback producer pipeline only.
/// </summary>
[Config(typeof(HostBenchmarkConfig))]
[MemoryDiagnoser]
public class MockedKafkaProduceBenchmark : HostBenchmark
{
    private const int OperationsCount = 30000;

    private static readonly SampleMessage Message = SampleMessage.Create();

    [Params(false, true)]
    public bool TraceBaggage { get; set; }

    [Benchmark(Description = "Produce", OperationsPerInvoke = OperationsCount)]
    public async Task PublishAsync()
    {
        // The activity is started in the benchmark, since the current activity doesn't reliably flow from the setup
        using Activity? activity = TraceBaggage ? BenchmarkActivity.StartWithBaggage() : null;

        for (int i = 0; i < OperationsCount; i++)
        {
            await Publisher.PublishAsync(Message);
        }
    }

    protected override void ConfigureSilverback(SilverbackBuilder silverbackBuilder) => silverbackBuilder
        .WithConnectionToMessageBroker(options => options.AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
        .AddKafkaClients(clients => clients
            .WithBootstrapServers("PLAINTEXT://benchmark")
            .AddProducer(producer => producer
                .Produce<SampleMessage>(endpoint => endpoint.ProduceTo("benchmark"))));
}
