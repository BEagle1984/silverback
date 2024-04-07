// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Performance.TestTypes;
using Silverback.Tests.Types;

namespace Silverback.Tests.Performance;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class JsonMessageSerializerBenchmark
{
    private readonly NewtonsoftJsonMessageSerializer _newtonsoftSerializer = new();

    private readonly JsonMessageSerializer _serializer = new();

    private readonly MessageHeaderCollection _messageHeaderCollection = [];

    private readonly ProducerEndpoint _producerEndpoint = new TestProducerEndpoint("Name", new TestProducerEndpointConfiguration());

    [Benchmark(Baseline = true, Description = "Newtonsoft based JsonMessageSerializer")]
    [BenchmarkCategory("Serialize")]
    public async Task SerializeAsyncUsingLegacySerializer() =>
        await _newtonsoftSerializer.SerializeAsync(WeekWhetherForecastsEvent.Sample, _messageHeaderCollection, _producerEndpoint);

    [Benchmark(Description = "New System.Text based JsonMessageSerializer")]
    [BenchmarkCategory("Serialize")]
    public async Task SerializeUsingNewSerializer() =>
        await _serializer.SerializeAsync(WeekWhetherForecastsEvent.Sample, _messageHeaderCollection, _producerEndpoint);

    [Benchmark(Baseline = true, Description = "Newtonsoft based JsonMessageSerializer")]
    [BenchmarkCategory("Deserialize")]
    public async Task DeserializeUsingLegacySerializer() =>
        await _newtonsoftSerializer.SerializeAsync(WeekWhetherForecastsEvent.Sample, _messageHeaderCollection, _producerEndpoint);

    [Benchmark(Description = "New System.Text based JsonMessageSerializer")]
    [BenchmarkCategory("Deserialize")]
    public async Task DeserializeUsingNewSerializer() =>
        await _serializer.SerializeAsync(WeekWhetherForecastsEvent.Sample, _messageHeaderCollection, _producerEndpoint);
}
