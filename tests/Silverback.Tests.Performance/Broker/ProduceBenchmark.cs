// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Logging;
using Silverback.Tests.Types.Domain;
using Silverback.Util;

namespace Silverback.Tests.Performance.Broker;

// 3.0.0
//
// |          Method |         Mean |      Error |     StdDev |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
// |---------------- |-------------:|-----------:|-----------:|-------:|-------:|------:|----------:|
// |     GetProducer |     34.05 ns |   0.161 ns |   0.150 ns | 0.0023 |      - |     - |      24 B |
// |         Produce | 28,361.58 ns | 375.549 ns | 332.914 ns | 0.7019 | 0.3357 |     - |    7598 B |
// |    ProduceAsync |  7,628.42 ns |  36.306 ns |  33.960 ns | 0.6027 | 0.2975 |     - |    6320 B |
// |      RawProduce |  4,591.11 ns |  32.165 ns |  30.087 ns | 0.2823 | 0.0076 |     - |    2962 B |
// | RawProduceAsync |  1,258.53 ns |  13.687 ns |  12.133 ns | 0.1755 | 0.0057 |     - |    1840 B |
//
// 3.3.0
//
// |                        Method |         Mean |      Error |     StdDev |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
// |------------------------------ |-------------:|-----------:|-----------:|-------:|-------:|------:|----------:|
// |                   GetProducer |     30.96 ns |   0.341 ns |   0.302 ns | 0.0023 |      - |     - |      24 B |
// |                       Produce | 40,918.81 ns | 545.400 ns | 510.168 ns | 0.9155 | 0.2441 |     - |  10,019 B |
// |                  ProduceAsync | 13,958.89 ns | 113.138 ns |  94.475 ns | 0.8392 | 0.2136 |     - |   8,816 B |
// |                    RawProduce |  2,941.50 ns |  36.635 ns |  32.476 ns | 0.1984 | 0.0648 |     - |   2,096 B |
// |               RawProduceAsync |  1,408.11 ns |  27.288 ns |  31.425 ns | 0.1373 | 0.0343 |     - |   1,448 B |
// | ProduceAsyncWithoutValidation | 11,416.72 ns | 103.036 ns |  80.444 ns | 0.7324 | 0.2441 |     - |   7,760 B |
[MemoryDiagnoser]
public class ProduceBenchmark
{
    private static readonly TestEventOne TestEventOne = new()
    {
        Content = "Benchmark this!"
    };

    private static readonly byte[] RawContent =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10
    ];

    private readonly IProducer _producer;

    private readonly IProducer _producerWithoutValidation;

    public ProduceBenchmark()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://benchmark")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo("benchmarks"))
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo("benchmarks-2").DisableMessageValidation()))));

        BrokerClientsConnectorService clientsConnectorService = serviceProvider.GetServices<IHostedService>().OfType<BrokerClientsConnectorService>().Single();
        clientsConnectorService.StartAsync(CancellationToken.None).SafeWait();

        IProducerCollection producerCollection = serviceProvider.GetRequiredService<IProducerCollection>();
        _producer = producerCollection[0];
        _producerWithoutValidation = producerCollection[^1];

        Activity activity = new("Benchmark");
        activity.Start();
    }

    [Benchmark]
    public void Produce() => _producer.Produce(TestEventOne);

    [Benchmark]
    public async Task ProduceAsync() => await _producer.ProduceAsync(TestEventOne);

    [Benchmark]
    public void RawProduce() => _producer.RawProduce(RawContent);

    [Benchmark]
    public async Task RawProduceAsync() => await _producer.RawProduceAsync(RawContent);

    [Benchmark]
    public async Task ProduceAsyncWithoutValidation() => await _producerWithoutValidation.ProduceAsync(TestEventOne);
}
