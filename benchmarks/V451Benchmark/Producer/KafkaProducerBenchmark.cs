// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Benchmarks.V451.Producer;

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

    protected override void ConfigureSilverback(ISilverbackBuilder silverbackBuilder) => silverbackBuilder
        .WithConnectionToMessageBroker(broker => broker.AddKafka())
        .AddKafkaEndpoints(
            endpoints => endpoints
                .Configure(
                    config =>
                    {
                        config.BootstrapServers = "PLAINTEXT://localhost:9092";
                    })
                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo($"benchmark-{Guid.NewGuid():N}")));

    private record SampleEvent(string Text) : IIntegrationEvent;
}
