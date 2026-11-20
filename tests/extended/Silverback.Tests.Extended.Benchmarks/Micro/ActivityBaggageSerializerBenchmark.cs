// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Silverback.Messaging.Diagnostics;
using Silverback.Tests.Extended.Benchmarks.Common;

namespace Silverback.Tests.Extended.Benchmarks.Micro;

/// <summary>
///     Measures the serialization of the activity baggage to (and from) the header value.
/// </summary>
[Config(typeof(MicroBenchmarkConfig))]
[MemoryDiagnoser]
public class ActivityBaggageSerializerBenchmark
{
    private KeyValuePair<string, string?>[] _baggage = [];

    private string _serializedBaggage = "";

    [Params(3, 10)]
    public int ItemsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _baggage = [.. Enumerable.Range(1, ItemsCount).Select(index => new KeyValuePair<string, string?>($"key-{index}", $"value-{index}"))];
        _serializedBaggage = ActivityBaggageSerializer.Serialize(_baggage);
    }

    [Benchmark]
    public string Serialize() => ActivityBaggageSerializer.Serialize(_baggage);

    [Benchmark]
    public IReadOnlyCollection<KeyValuePair<string, string>> Deserialize() => ActivityBaggageSerializer.Deserialize(_serializedBaggage);
}
