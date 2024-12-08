// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Benchmarks.Latest.Producer;

[SimpleJob]
[MemoryDiagnoser]
[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Test code")]
public class KafkaProducerBenchmark : Benchmark
{
    private readonly IIntegrationEvent[] _events = Enumerable.Range(1, 1000).Select(index => (IIntegrationEvent)new SampleEvent($"Test {index}")).ToArray();

    [Benchmark(Description = "PublishAsync inside foreach", Baseline = true)]
    public async Task PublishAsync()
    {
        foreach (IIntegrationEvent eventMessage in _events)
        {
            await Publisher.PublishAsync(eventMessage);
        }
    }

    [Benchmark(Description = "WrapAndPublishBatchAsync")]
    public async Task WrapAndPublishAsync() => await Publisher.WrapAndPublishBatchAsync(_events);

    protected override void ConfigureSilverback(SilverbackBuilder silverbackBuilder) => silverbackBuilder
        .WithConnectionToMessageBroker(broker => broker.AddKafka())
        .AddKafkaClients(
            clients => clients
                .AddProducer(
                    producer => producer
                        .WithBootstrapServers("PLAINTEXT://localhost:9092")
                        .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo($"benchmark-{Guid.NewGuid():N}"))));

    private record SampleEvent(string Text) : IIntegrationEvent;
}
