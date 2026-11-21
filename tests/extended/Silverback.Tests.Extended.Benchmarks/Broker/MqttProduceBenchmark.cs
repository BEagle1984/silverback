// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Linq;
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
///     Measures producing a ~200 B JSON message to the mocked MQTT broker, directly via the producer and via the publisher.
///     The ~64 KB payload is deliberately measured only for Kafka (see <see cref="KafkaProduceLargePayloadBenchmark" />), since the payload
///     handling is shared and not broker specific.
/// </summary>
[Config(typeof(HostBenchmarkConfig))]
[MemoryDiagnoser]
public class MqttProduceBenchmark : HostBenchmarkBase
{
    private const int OperationsCount = 30000;

    private static readonly BenchmarkMessage Message = BenchmarkMessage.Create(BenchmarkMessage.SmallPayloadLength);

    private IProducer? _producer;

    [Params(ProducerHeadersKind.None, ProducerHeadersKind.FiveStatic, ProducerHeadersKind.FiveValueProvider)]
    public ProducerHeadersKind Headers { get; set; }

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
        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
        .AddMqttClients(clients => clients
            .ConnectViaTcp("benchmark-mqtt-broker")
            .AddClient(client => client
                .WithClientId("benchmark")
                .Produce<BenchmarkMessage>(endpoint => endpoint
                    .ProduceTo("benchmark")
                    .SerializeAsJson()
                    .AddHeaders(Headers))));

    protected override void OnHostStarted()
    {
        ScopedServices.GetRequiredService<IMqttTestingHelper>().WaitUntilConnectedAsync().AsTask().SafeWait();

        _producer = ScopedServices.GetRequiredService<IProducerCollection>().Single();
    }
}
