// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Benchmarks.Latest.Producer;

[SimpleJob]
[MemoryDiagnoser]
[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Test code")]
public class KafkaConsumerPipelineBenchmark : Benchmark, IDisposable
{
    private const int MessagesCount = 1000;

    private readonly string _standardTopicName = $"standard-{Guid.NewGuid():N}";

    private readonly string _batchTopicName = $"batch-{Guid.NewGuid():N}";

    private readonly CountdownEvent _standardCountdownEvent = new(MessagesCount);

    private readonly CountdownEvent _batchCountdownEvent = new(MessagesCount);

    public override void Setup()
    {
        base.Setup();

        IConsumerCollection consumers = Host.Services.GetRequiredService<IConsumerCollection>();
        foreach (IConsumer consumer in consumers)
        {
            consumer.StopAsync().AsTask().Wait();
        }

        for (int i = 0; i < MessagesCount; i++)
        {
            Publisher.Publish(new StandardEvent($"Test {i}"));
            Publisher.Publish(new BatchEvent($"Test {i}"));
        }
    }

    [Benchmark(Description = "Standard", Baseline = true)]
    public void StandardConsume()
    {
        _standardCountdownEvent.Reset();
        IConsumerCollection consumers = Host.Services.GetRequiredService<IConsumerCollection>();
        foreach (IConsumer consumer in consumers)
        {
            consumer.StartAsync().AsTask().Wait();
        }

        _standardCountdownEvent.Wait();
    }

    [Benchmark(Description = "Batch")]
    public void BatchConsume()
    {
        _batchCountdownEvent.Reset();
        IConsumerCollection consumers = Host.Services.GetRequiredService<IConsumerCollection>();
        foreach (IConsumer consumer in consumers)
        {
            consumer.StartAsync().AsTask().Wait();
        }

        _batchCountdownEvent.Wait();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        _standardCountdownEvent.Dispose();
        _batchCountdownEvent.Dispose();
    }

    protected override void ConfigureSilverback(SilverbackBuilder silverbackBuilder) => silverbackBuilder
        .WithConnectionToMessageBroker(broker => broker.AddKafka())
        .AddKafkaClients(
            clients => clients
                .WithBootstrapServers("PLAINTEXT://localhost:9092")
                .AddProducer(
                    producer => producer
                        .Produce<StandardEvent>(endpoint => endpoint.ProduceTo(_standardTopicName))
                        .Produce<BatchEvent>(endpoint => endpoint.ProduceTo(_batchTopicName)))
                .AddConsumer(
                    consumer => consumer
                        .WithGroupId("benchmark")
                        .WithClientId("benchmark")
                        .AutoResetOffsetToEarliest()
                        .Consume<StandardEvent>(
                            endpoint => endpoint
                                .ConsumeFrom(_standardTopicName))
                        .Consume<BatchEvent>(
                            endpoint => endpoint
                                .ConsumeFrom(_batchTopicName)
                                .EnableBatchProcessing(100, TimeSpan.FromMilliseconds(100)))))
        .AddSingletonSubscriber<Subscriber>(_ => new Subscriber(this));

    private class Subscriber
    {
        private readonly KafkaConsumerPipelineBenchmark _benchmark;

        public Subscriber(KafkaConsumerPipelineBenchmark benchmark)
        {
            _benchmark = benchmark;
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Called by Silverback")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Called by Silverback")]
        public async Task HandleAsync(StandardEvent message)
        {
            await Task.Delay(50);
            _benchmark._standardCountdownEvent.Signal();
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Called by Silverback")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Called by Silverback")]
        public async Task HandleAsync(IAsyncEnumerable<BatchEvent> messages)
        {
            int count = 0;

            await foreach (BatchEvent message in messages)
            {
                await Task.Delay(10);
                count++;
            }

            await Task.Delay(100);

            _benchmark._batchCountdownEvent.Signal(count);
        }
    }

    private record StandardEvent(string Text) : IIntegrationEvent;

    private record BatchEvent(string Text) : IIntegrationEvent;
}
