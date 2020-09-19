// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Performance.TestTypes;

namespace Silverback.Tests.Performance
{
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [MemoryDiagnoser]
    public class JsonMessageSerializerBenchmark
    {
        private readonly NewtonsoftJsonMessageSerializer _newtonsoftSerializer = new NewtonsoftJsonMessageSerializer();

        private readonly JsonMessageSerializer _serializer = new JsonMessageSerializer();

        private readonly MessageHeaderCollection _messageHeaderCollection = new MessageHeaderCollection();

        private readonly MessageSerializationContext _messageSerializationContext =
            new MessageSerializationContext(new TestProducerEndpoint("Name"));

        [Benchmark(Baseline = true, Description = "Newtonsoft based JsonMessageSerializer")]
        [BenchmarkCategory("Serialize")]
        public async Task SerializeAsyncUsingLegacySerializer()
        {
            await _newtonsoftSerializer.SerializeAsync(
                WeekWhetherForecastsEvent.Sample,
                _messageHeaderCollection,
                _messageSerializationContext);
        }

        [Benchmark(Description = "New System.Text based JsonMessageSerializer")]
        [BenchmarkCategory("Serialize")]
        public async Task SerializeUsingNewSerializer()
        {
            await _serializer.SerializeAsync(
                WeekWhetherForecastsEvent.Sample,
                _messageHeaderCollection,
                _messageSerializationContext);
        }

        [Benchmark(Baseline = true, Description = "Newtonsoft based JsonMessageSerializer")]
        [BenchmarkCategory("Deserialize")]
        public async Task DeserializeUsingLegacySerializer()
        {
            await _newtonsoftSerializer.SerializeAsync(
                WeekWhetherForecastsEvent.Sample,
                _messageHeaderCollection,
                _messageSerializationContext);
        }

        [Benchmark(Description = "New System.Text based JsonMessageSerializer")]
        [BenchmarkCategory("Deserialize")]
        public async Task DeserializeUsingNewSerializer()
        {
            await _serializer.SerializeAsync(
                WeekWhetherForecastsEvent.Sample,
                _messageHeaderCollection,
                _messageSerializationContext);
        }
    }
}
