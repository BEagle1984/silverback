// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Extended.Benchmarks.Common;

namespace Silverback.Tests.Extended.Benchmarks.Broker;

/// <summary>
///     Measures producing a ~200 B JSON message to the mocked Kafka broker, directly via the producer (with and without message
///     validation, and the raw bytes without serialization) and via the publisher.
/// </summary>
[Config(typeof(HostBenchmarkConfig))]
[MemoryDiagnoser]
public class KafkaProduceBenchmark : HostBenchmarkBase
{
    private const int OperationsCount = 30000;

    // The raw produce is much faster: more operations keep the iterations long enough to be measured reliably
    private const int RawOperationsCount = 200000;

    private static readonly BenchmarkMessage Message = BenchmarkMessage.Create(BenchmarkMessage.SmallPayloadLength);

    private static readonly byte[] RawContent = new byte[200];

    private IProducer? _producer;

    private IProducer? _producerWithoutValidation;

    [Params(ProducerHeadersKind.None, ProducerHeadersKind.FiveStatic, ProducerHeadersKind.FiveValueProvider)]
    public ProducerHeadersKind Headers { get; set; }

    [Benchmark(OperationsPerInvoke = OperationsCount)]
    public void Produce()
    {
        for (int i = 0; i < OperationsCount; i++)
        {
            _producer!.Produce(Message);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsCount)]
    public async Task ProduceAsync()
    {
        for (int i = 0; i < OperationsCount; i++)
        {
            await _producer!.ProduceAsync(Message);
        }
    }

    // Always without headers: compare it to ProduceAsync with Headers = None
    [Benchmark(OperationsPerInvoke = OperationsCount)]
    public async Task ProduceAsyncWithoutValidation()
    {
        for (int i = 0; i < OperationsCount; i++)
        {
            await _producerWithoutValidation!.ProduceAsync(Message);
        }
    }

    [Benchmark(OperationsPerInvoke = RawOperationsCount)]
    public void RawProduce()
    {
        for (int i = 0; i < RawOperationsCount; i++)
        {
            _producer!.RawProduce(RawContent);
        }
    }

    [Benchmark(OperationsPerInvoke = RawOperationsCount)]
    public async Task RawProduceAsync()
    {
        for (int i = 0; i < RawOperationsCount; i++)
        {
            await _producer!.RawProduceAsync(RawContent);
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

    // The activity is started in the benchmark, since the current activity doesn't reliably flow from the setup
    [Benchmark(OperationsPerInvoke = OperationsCount)]
    public async Task ProduceAsyncUnderActivityWithBaggage()
    {
        using Activity activity = BenchmarkActivity.StartWithBaggage();

        for (int i = 0; i < OperationsCount; i++)
        {
            await _producer!.ProduceAsync(Message);
        }
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
            .AddProducer(
                "without-validation",
                producer => producer
                    .Produce<UnroutedBenchmarkMessage>(endpoint => endpoint
                        .ProduceTo("benchmark-without-validation")
                        .SerializeAsJson()
                        .DisableMessageValidation())));

    protected override void OnHostStarted()
    {
        IProducerCollection producers = ScopedServices.GetRequiredService<IProducerCollection>();
        _producer = producers.GetProducerForEndpoint("benchmark");
        _producerWithoutValidation = producers.GetProducerForEndpoint("benchmark-without-validation");
    }

    // Only used to register a second endpoint that the publisher never routes to (it is produced to directly)
    private sealed record UnroutedBenchmarkMessage;
}
