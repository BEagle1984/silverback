// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Performance.TestTypes;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;

namespace Silverback.Tests.Performance;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class JsonMessageSerializerBenchmark
{
    private readonly NewtonsoftJsonMessageSerializer<TestEventOne> _newtonsoftSerializer = new();

    private readonly JsonMessageSerializer<TestEventOne> _serializer = new();

    private readonly MessageHeaderCollection _messageHeaderCollection = new();

    private readonly ProducerEndpoint _producerEndpoint = new TestProducerEndpoint("Name", new TestProducerEndpointConfiguration());

    [Benchmark(Baseline = true, Description = "Newtonsoft based JsonMessageSerializer")]
    [BenchmarkCategory("Serialize")]
    public async Task SerializeAsyncUsingLegacySerializer()
    {
        await _newtonsoftSerializer.SerializeAsync(WeekWhetherForecastsEvent.Sample, _messageHeaderCollection, _producerEndpoint);
    }

    [Benchmark(Description = "New System.Text based JsonMessageSerializer")]
    [BenchmarkCategory("Serialize")]
    public async Task SerializeUsingNewSerializer()
    {
        await _serializer.SerializeAsync(WeekWhetherForecastsEvent.Sample, _messageHeaderCollection, _producerEndpoint);
    }

    [Benchmark(Baseline = true, Description = "Newtonsoft based JsonMessageSerializer")]
    [BenchmarkCategory("Deserialize")]
    public async Task DeserializeUsingLegacySerializer()
    {
        await _newtonsoftSerializer.SerializeAsync(WeekWhetherForecastsEvent.Sample, _messageHeaderCollection, _producerEndpoint);
    }

    [Benchmark(Description = "New System.Text based JsonMessageSerializer")]
    [BenchmarkCategory("Deserialize")]
    public async Task DeserializeUsingNewSerializer()
    {
        await _serializer.SerializeAsync(WeekWhetherForecastsEvent.Sample, _messageHeaderCollection, _producerEndpoint);
    }
}
