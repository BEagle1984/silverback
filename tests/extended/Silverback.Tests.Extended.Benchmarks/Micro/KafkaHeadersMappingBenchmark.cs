// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Silverback.Messaging.Messages;
using Silverback.Tests.Extended.Benchmarks.Common;

namespace Silverback.Tests.Extended.Benchmarks.Micro;

/// <summary>
///     Measures the mapping between the Silverback and the Confluent.Kafka headers (the internal headers are not mapped).
/// </summary>
[Config(typeof(MicroBenchmarkConfig))]
[MemoryDiagnoser]
public class KafkaHeadersMappingBenchmark
{
    private MessageHeaderCollection _silverbackHeaders = [];

    private Confluent.Kafka.Headers _confluentHeaders = [];

    [Params(5, 20)]
    public int HeadersCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _silverbackHeaders = [.. Enumerable.Range(1, HeadersCount).Select(index => new MessageHeader($"header-{index}", $"value-{index}"))];
        _silverbackHeaders.Add(DefaultMessageHeaders.InternalHeadersPrefix + "internal", "not mapped");

        _confluentHeaders = _silverbackHeaders.ToConfluentHeaders();
    }

    [Benchmark]
    public Confluent.Kafka.Headers ToConfluentHeaders() => _silverbackHeaders.ToConfluentHeaders();

    [Benchmark]
    public IReadOnlyCollection<MessageHeader> ToSilverbackHeaders() => _confluentHeaders.ToSilverbackHeaders();
}
