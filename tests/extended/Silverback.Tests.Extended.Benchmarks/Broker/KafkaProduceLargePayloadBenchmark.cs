// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Extended.Benchmarks.Common;

namespace Silverback.Tests.Extended.Benchmarks.Broker;

/// <summary>
///     Measures producing a ~64 KB JSON message to the mocked Kafka broker (the operations per iteration are fewer than in
///     <see cref="KafkaProduceBenchmark" />, to bound the memory retained by the mocked topic).
/// </summary>
[Config(typeof(HostBenchmarkConfig))]
[MemoryDiagnoser]
public class KafkaProduceLargePayloadBenchmark : HostBenchmarkBase
{
    private const int OperationsCount = 500;

    private static readonly BenchmarkMessage Message = BenchmarkMessage.Create(BenchmarkMessage.LargePayloadLength);

    private IProducer? _producer;

    [Benchmark(OperationsPerInvoke = OperationsCount)]
    public async Task ProduceAsync()
    {
        for (int i = 0; i < OperationsCount; i++)
        {
            await _producer!.ProduceAsync(Message);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsCount)]
    public async Task PublishAsync()
    {
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
                .Produce<BenchmarkMessage>(endpoint => endpoint
                    .ProduceTo("benchmark")
                    .SerializeAsJson())));

    protected override void OnHostStarted() => _producer = ScopedServices.GetRequiredService<IProducerCollection>().Single();
}
