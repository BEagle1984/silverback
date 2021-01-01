// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Tests.Logging;
using Silverback.Tests.Types.Domain;

namespace Silverback.Tests.Performance.Broker
{
    // Baseline v3.0.0
    //
    // |          Method |         Mean |      Error |     StdDev |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
    // |---------------- |-------------:|-----------:|-----------:|-------:|-------:|------:|----------:|
    // |     GetProducer |     30.41 ns |   0.388 ns |   0.363 ns | 0.0023 |      - |     - |      24 B |
    // |         Produce | 30,417.40 ns | 439.792 ns | 411.382 ns | 0.7629 | 0.3662 |     - |    8047 B |
    // |    ProduceAsync |  8,709.56 ns |  62.243 ns |  58.222 ns | 0.6409 | 0.3204 |     - |    6768 B |
    // |      RawProduce |  3,968.06 ns |  52.066 ns |  48.702 ns | 0.2213 | 0.0076 |     - |    2345 B |
    // | RawProduceAsync |    931.43 ns |   3.886 ns |   3.445 ns | 0.1163 | 0.0038 |     - |    1224 B |
    //
    // 3.0.0 - High-performance logging
    //
    // |          Method |         Mean |      Error |     StdDev |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
    // |---------------- |-------------:|-----------:|-----------:|-------:|-------:|------:|----------:|
    // |     GetProducer |     34.05 ns |   0.161 ns |   0.150 ns | 0.0023 |      - |     - |      24 B |
    // |         Produce | 28,361.58 ns | 375.549 ns | 332.914 ns | 0.7019 | 0.3357 |     - |    7598 B |
    // |    ProduceAsync |  7,628.42 ns |  36.306 ns |  33.960 ns | 0.6027 | 0.2975 |     - |    6320 B |
    // |      RawProduce |  4,591.11 ns |  32.165 ns |  30.087 ns | 0.2823 | 0.0076 |     - |    2962 B |
    // | RawProduceAsync |  1,258.53 ns |  13.687 ns |  12.133 ns | 0.1755 | 0.0057 |     - |    1840 B |
    [MemoryDiagnoser]
    public class ProduceBenchmark
    {
        private static readonly TestEventOne TestEventOne = new()
        {
            Content = "Benchmark this!"
        };

        private static readonly byte[] RawContent =
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10
        };

        private readonly IBroker _broker;

        private readonly IProducer _producer;

        private readonly IProducerEndpoint _endpoint;

        public ProduceBenchmark()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .Configure(config => { config.BootstrapServers = "PLAINTEXT://benchmark"; })
                            .AddOutbound<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo("benchmarks"))));

            _broker = serviceProvider.GetRequiredService<IBroker>();

            _broker.ConnectAsync().Wait();

            _producer = _broker.Producers[0];
            _endpoint = _producer.Endpoint;

            var activity = new Activity("Benchmark");
            activity.Start();
        }

        [Benchmark]
        public void GetProducer() => _broker.GetProducer(_endpoint);

        [Benchmark]
        public void Produce() => _producer.Produce(TestEventOne);

        [Benchmark]
        public Task ProduceAsync() => _producer.ProduceAsync(TestEventOne);

        [Benchmark]
        public void RawProduce() => _producer.RawProduce(RawContent);

        [Benchmark]
        public Task RawProduceAsync() => _producer.RawProduceAsync(RawContent);
    }
}
